using NetTopologySuite.Geometries;

namespace DozorLogger.Services;

public static class GeoCalculator
{
    public const double MetersPerDegreeLat = 111140.0;
    public const double MetersPerDegreeLon = 72280.0;

    public static Coordinate ToMetricCoordinate(double lat, double lon)
    {
        return new Coordinate(lon * MetersPerDegreeLon, lat * MetersPerDegreeLat);
    }

    public static double CalculateDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        var p1 = ToMetricCoordinate(lat1, lon1);
        var p2 = ToMetricCoordinate(lat2, lon2);
        return p1.Distance(p2);
    }

    public static bool IsAzimuthMatching(double actualAzimuth, double? expectedAzimuth, double toleranceDegrees)
    {
        if (expectedAzimuth is null or < 0) return true;

        double diff = Math.Abs(actualAzimuth - expectedAzimuth.Value) % 360;
        double angleDiff = Math.Min(diff, 360 - diff);
        return angleDiff <= toleranceDegrees;
    }

    public static (double MinDistanceMeters, double T, double SegmentAzimuth) CalculateSegmentDistanceMeters(
        double latA, double lonA,
        double latB, double lonB,
        double targetLat, double targetLon)
    {
        var start = ToMetricCoordinate(latA, lonA);
        var end = ToMetricCoordinate(latB, lonB);
        var target = ToMetricCoordinate(targetLat, targetLon);

        var segment = new LineSegment(start, end);

        if (segment.Length < 1.0)
        {
            return (start.Distance(target), 0.0, 0.0);
        }

        double t = segment.ProjectionFactor(target);

        double minDistance = segment.Distance(target);

        double dx = end.X - start.X;
        double dy = end.Y - start.Y;
        double angleRad = Math.Atan2(dx, dy);
        double segAzimuth = (angleRad * 180.0 / Math.PI + 360.0) % 360.0;

        return (minDistance, t, segAzimuth);
    }
}
