using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DozorLogger.Configuration;
using DozorLogger.Models;

namespace DozorLogger.Services;

public interface IDozorClient
{
    Task<List<DozorBus>> GetActiveBusesAsync(CancellationToken cancellationToken = default);
}

public class DozorClient : IDozorClient
{
    private readonly HttpClient _httpClient;
    private readonly TrackingOptions _options;
    private readonly ILogger<DozorClient> _logger;
    private bool _sessionInitialized;

    public DozorClient(HttpClient httpClient, IOptions<TrackingOptions> options, ILogger<DozorClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    private async Task EnsureSessionAsync(CancellationToken cancellationToken)
    {
        if (_sessionInitialized) return;
        try
        {
            await _httpClient.GetAsync("https://city.dozor.tech/ua/cherkasy/city", cancellationToken);
            _sessionInitialized = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Помилка ініціалізації сесії Dozor: {Message}", ex.Message);
        }
    }

    public async Task<List<DozorBus>> GetActiveBusesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSessionAsync(cancellationToken);

        string url = _options.GetEffectiveDozorUrl();
        try
        {
            var dozorResponse = await _httpClient.GetFromJsonAsync<DozorResponse>(url, cancellationToken);
            if (dozorResponse?.Data == null || dozorResponse.Data.Count == 0)
            {
                _sessionInitialized = false;
                await EnsureSessionAsync(cancellationToken);
                dozorResponse = await _httpClient.GetFromJsonAsync<DozorResponse>(url, cancellationToken);
                if (dozorResponse?.Data == null || dozorResponse.Data.Count == 0)
                {
                    return new List<DozorBus>();
                }
            }

            return dozorResponse.Data
                .Where(r => r.Buses != null)
                .SelectMany(r => r.Buses!)
                .Where(d => d.Location != null)
                .ToList();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning("Network error connecting to Dozor API: {Message}", ex.Message);
            return new List<DozorBus>();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error querying Dozor API at {Url}", url);
            return new List<DozorBus>();
        }
    }
}