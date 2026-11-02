using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DozorLogger.Configuration;
using DozorLogger.Models;

namespace DozorLogger.Services;

public interface IGoogleSheetsWebhookService
{
    bool IsEnabled { get; }
    Task<bool> SendObservationAsync(StopObservation observation, CancellationToken cancellationToken = default);
}

public class GoogleSheetsWebhookService : IGoogleSheetsWebhookService
{
    private readonly HttpClient _httpClient;
    private readonly TrackingOptions _options;
    private readonly ILogger<GoogleSheetsWebhookService> _logger;

    public bool IsEnabled => !string.IsNullOrWhiteSpace(_options.GoogleSheetsWebhookUrl);

    public GoogleSheetsWebhookService(HttpClient httpClient, IOptions<TrackingOptions> options, ILogger<GoogleSheetsWebhookService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> SendObservationAsync(StopObservation observation, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled) return false;

        try
        {
            var payload = new
            {
                index = observation.Index,
                time = observation.LocalTime.ToString("yyyy-MM-dd HH:mm:ss"),
                interval = observation.IntervalMinutes.HasValue ? observation.IntervalMinutes.Value.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) : "-",
                plate = observation.LicensePlate,
                vehicleId = observation.VehicleId,
                distance = observation.DistanceMeters.ToString("F1", System.Globalization.CultureInfo.InvariantCulture),
                speed = observation.SpeedKmH.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)
            };

            var response = await _httpClient.PostAsJsonAsync(_options.GoogleSheetsWebhookUrl, payload, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Помилка відправки на Webhook Google Таблиці: {Message}", ex.Message);
            return false;
        }
    }
}

