using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DozorLogger.Configuration;
using DozorLogger.Services;

namespace DozorLogger;

public class Worker : BackgroundService
{
    private readonly IDozorClient _dozorClient;
    private readonly IStopDetector _stopDetector;
    private readonly IEventLoggerService _eventLogger;
    private readonly TrackingOptions _options;
    private readonly ILogger<Worker> _logger;

    public Worker(
        IDozorClient dozorClient,
        IStopDetector stopDetector,
        IEventLoggerService eventLogger,
        IOptions<TrackingOptions> options,
        ILogger<Worker> logger)
    {
        _dozorClient = dozorClient;
        _stopDetector = stopDetector;
        _eventLogger = eventLogger;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("==================================================");
        _logger.LogInformation("Запуск відстеження маршруту: {RouteId}", _options.RouteId);
        _logger.LogInformation("Координати зупинки: {Lat:F6}, {Lng:F6}", _options.TargetLatitude, _options.TargetLongitude);
        _logger.LogInformation("Радіус зупинки: {Radius}м", _options.StopRadiusMeters);
        _logger.LogInformation("==================================================");

        int pollCount = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                pollCount++;
                var buses = await _dozorClient.GetActiveBusesAsync(stoppingToken);

                if (pollCount % 12 == 1)
                {
                    int totalObs = _stopDetector.GetAllObservations().Count;
                    _logger.LogInformation("Статус: активних автобусів {ActiveCount} | Зафіксовано зупинок: {ObsCount}",
                        buses.Count, totalObs);
                }

                var newObs = _stopDetector.DetectArrivals(buses);
                if (newObs.Count > 0)
                {
                    await _eventLogger.LogObservationsAsync(newObs, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError("Помилка циклу відстеження: {Message}", ex.Message);
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("Сервіс зупинено.");
    }
}
