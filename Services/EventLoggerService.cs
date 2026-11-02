using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DozorLogger.Configuration;
using DozorLogger.Models;

namespace DozorLogger.Services;

public interface IEventLoggerService
{
    Task LogObservationsAsync(IEnumerable<StopObservation> observations, CancellationToken cancellationToken = default);
}

public class EventLoggerService : IEventLoggerService
{
    private readonly TrackingOptions _options;
    private readonly IGoogleSheetsWebhookService _webhookService;
    private readonly ILogger<EventLoggerService> _logger;

    public EventLoggerService(
        IOptions<TrackingOptions> options,
        IGoogleSheetsWebhookService webhookService,
        ILogger<EventLoggerService> logger)
    {
        _options = options.Value;
        _webhookService = webhookService;
        _logger = logger;
    }

    public async Task LogObservationsAsync(
        IEnumerable<StopObservation> newObservations,
        CancellationToken cancellationToken = default)
    {
        var list = newObservations.ToList();
        if (list.Count == 0) return;

        try
        {
            string csvPath = _options.LocalLogFilePath;
            string? dir = Path.GetDirectoryName(csvPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            bool writeHeader = !File.Exists(csvPath) || new FileInfo(csvPath).Length == 0;
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = writeHeader
            };

            await using (var stream = File.Open(csvPath, FileMode.Append, FileAccess.Write, FileShare.Read))
            await using (var writer = new StreamWriter(stream, System.Text.Encoding.UTF8))
            await using (var csv = new CsvWriter(writer, config))
            {
                csv.Context.RegisterClassMap<StopObservationMap>();
                await csv.WriteRecordsAsync(list, cancellationToken);
            }

            string statusText = "Локально: ОК";
            if (_webhookService.IsEnabled)
            {
                bool anyError = false;
                foreach (var obs in list)
                {
                    bool ok = await _webhookService.SendObservationAsync(obs, cancellationToken);
                    if (!ok) anyError = true;
                }
                statusText = anyError ? "Таблиця: Помилка" : "Таблиця: ОК";
            }

            foreach (var obs in list)
            {
                string timeStr = obs.LocalTime.ToString("yyyy-MM-dd HH:mm:ss");
                string intervalText = obs.IntervalMinutes.HasValue
                    ? $"Інтервал: {obs.IntervalMinutes.Value:F1} хв | "
                    : string.Empty;

                _logger.LogInformation(
                    "[{Time}] Зупинка #{Index} | {Interval}{Plate} | {Status}",
                    timeStr, obs.Index, intervalText, obs.LicensePlate, statusText
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Помилка запису: {Message}", ex.Message);
        }
    }
}
