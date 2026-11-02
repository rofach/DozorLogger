using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TimeZoneConverter;
using DozorLogger.Configuration;
using DozorLogger.Models;

namespace DozorLogger.Services;

public interface IStopDetector
{
    List<StopObservation> DetectArrivals(List<DozorBus> buses);
    IReadOnlyList<StopObservation> GetAllObservations();
}

public class StopDetector : IStopDetector
{
    private class PreviousPosition
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }

    private readonly TrackingOptions _options;
    private readonly ILogger<StopDetector> _logger;

    private readonly Dictionary<int, PreviousPosition> _lastKnownPositions = new();
    private readonly Dictionary<int, DateTimeOffset> _lastRecordedArrivals = new();
    private readonly List<StopObservation> _allObservations = new();
    private DateTimeOffset? _previousArrivalUtc;

    private readonly TimeSpan _vehicleCooldown = TimeSpan.FromMinutes(7);
    private static readonly TimeSpan MaxSegmentDuration = TimeSpan.FromSeconds(45);

    public StopDetector(IOptions<TrackingOptions> options, ILogger<StopDetector> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public List<StopObservation> DetectArrivals(List<DozorBus> buses)
    {
        var newObservations = new List<StopObservation>();
        var nowUtc = DateTimeOffset.UtcNow;

        foreach (var bus in buses)
        {
            if (bus.Location == null) continue;

            if (TryDetectArrival(bus, nowUtc, out double effectiveDistance, out DateTimeOffset arrivalUtc))
            {
                if (!IsOnCooldown(bus.Id, nowUtc))
                {
                    _lastRecordedArrivals[bus.Id] = nowUtc;
                    var observation = CreateObservation(bus, effectiveDistance, arrivalUtc);
                    newObservations.Add(observation);
                }
            }

            UpdateLastKnownPosition(bus, nowUtc);
        }

        return newObservations;
    }

    private bool TryDetectArrival(DozorBus bus, DateTimeOffset nowUtc, out double effectiveDistance, out DateTimeOffset arrivalUtc)
    {
        if (TryDetectTrajectoryArrival(bus, nowUtc, out effectiveDistance, out arrivalUtc))
        {
            return true;
        }

        if (_options.EnableDirectPointCheck && TryDetectPointArrival(bus, nowUtc, out effectiveDistance, out arrivalUtc))
        {
            return true;
        }

        effectiveDistance = 0;
        arrivalUtc = nowUtc;
        return false;
    }

    private bool TryDetectTrajectoryArrival(DozorBus bus, DateTimeOffset nowUtc, out double effectiveDistance, out DateTimeOffset arrivalUtc)
    {
        effectiveDistance = 0;
        arrivalUtc = nowUtc;

        if (!_lastKnownPositions.TryGetValue(bus.Id, out var prevPos))
            return false;

        var timeSincePrev = nowUtc - prevPos.Timestamp;
        if (timeSincePrev > MaxSegmentDuration)
            return false;

        var (minDist, t, segAzimuth) = GeoCalculator.CalculateSegmentDistanceMeters(
            prevPos.Latitude, prevPos.Longitude,
            bus.Location!.Latitude, bus.Location!.Longitude,
            _options.TargetLatitude, _options.TargetLongitude
        );

        if (t < 0.0 || t > 1.0 || minDist > _options.StopRadiusMeters)
            return false;

        bool matchesAzimuth = GeoCalculator.IsAzimuthMatching(segAzimuth, _options.ExpectedAzimuth, _options.AzimuthToleranceDegrees)
            || GeoCalculator.IsAzimuthMatching(bus.Azimuth, _options.ExpectedAzimuth, _options.AzimuthToleranceDegrees);

        if (!matchesAzimuth)
            return false;

        effectiveDistance = minDist;
        long interpolatedTicks = prevPos.Timestamp.Ticks + (long)(timeSincePrev.Ticks * t);
        arrivalUtc = new DateTimeOffset(interpolatedTicks, TimeSpan.Zero);

        _logger.LogInformation(
            "[Векторний алгоритм] Автобус {Plate} (ID {Id}): проїзд t={T:F2}, дистанція={Dist:F1}м, азимут={Az:F0}°",
            bus.LicensePlate, bus.Id, t, minDist, segAzimuth
        );

        return true;
    }

    private bool TryDetectPointArrival(DozorBus bus, DateTimeOffset nowUtc, out double effectiveDistance, out DateTimeOffset arrivalUtc)
    {
        effectiveDistance = 0;
        arrivalUtc = nowUtc;

        double distance = GeoCalculator.CalculateDistanceMeters(
            bus.Location!.Latitude, bus.Location!.Longitude,
            _options.TargetLatitude, _options.TargetLongitude
        );

        if (distance > _options.DirectPointRadiusMeters)
            return false;

        if (!GeoCalculator.IsAzimuthMatching(bus.Azimuth, _options.ExpectedAzimuth, _options.AzimuthToleranceDegrees))
            return false;

        effectiveDistance = distance;
        _logger.LogInformation(
            "[Перевірка точки] Автобус {Plate} (ID {Id}): всередині радіуса {Radius}м (дистанція={Dist:F1}м)",
            bus.LicensePlate, bus.Id, _options.DirectPointRadiusMeters, distance
        );

        return true;
    }

    private bool IsOnCooldown(int busId, DateTimeOffset nowUtc)
    {
        return _lastRecordedArrivals.TryGetValue(busId, out var lastArrival)
            && (nowUtc - lastArrival < _vehicleCooldown);
    }

    private StopObservation CreateObservation(DozorBus bus, double effectiveDistance, DateTimeOffset arrivalUtc)
    {
        double? interval = null;
        if (_previousArrivalUtc.HasValue)
        {
            interval = (arrivalUtc - _previousArrivalUtc.Value).TotalMinutes;
        }
        _previousArrivalUtc = arrivalUtc;

        int newIndex = _allObservations.Count + 1;
        var observation = new StopObservation
        {
            Index = newIndex,
            LocalTime = ConvertToKyivTime(arrivalUtc),
            IntervalMinutes = interval,
            LicensePlate = !string.IsNullOrWhiteSpace(bus.LicensePlate) ? bus.LicensePlate : $"ID-{bus.Id}",
            VehicleId = bus.Id,
            DistanceMeters = Math.Round(effectiveDistance, 1),
            SpeedKmH = bus.Speed,
            Azimuth = bus.Azimuth
        };

        _allObservations.Add(observation);
        return observation;
    }

    private void UpdateLastKnownPosition(DozorBus bus, DateTimeOffset nowUtc)
    {
        _lastKnownPositions[bus.Id] = new PreviousPosition
        {
            Latitude = bus.Location!.Latitude,
            Longitude = bus.Location!.Longitude,
            Timestamp = nowUtc
        };
    }

    public IReadOnlyList<StopObservation> GetAllObservations()
    {
        return _allObservations.ToList();
    }

    private static readonly TimeZoneInfo KyivTimeZone = TZConvert.GetTimeZoneInfo("Europe/Kyiv");

    public static DateTime ConvertToKyivTime(DateTimeOffset utcTime)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(utcTime.UtcDateTime, KyivTimeZone);
    }
}
