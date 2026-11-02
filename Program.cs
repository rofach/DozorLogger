using DotNetEnv;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using DozorLogger;
using DozorLogger.Configuration;
using DozorLogger.Services;

try
{
    Env.TraversePath().Load();
}
catch { }

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<TrackingOptions>(options =>
{
    builder.Configuration.GetSection(TrackingOptions.SectionName).Bind(options);

    if (GetEnv("DOZOR_URL") is { Length: > 0 } dozorUrl)
        options.DozorUrl = dozorUrl;

    if (GetEnvInt("ROUTE_ID") is { } routeId)
        options.RouteId = routeId;

    if ((GetEnvDouble("TARGET_LAT") ?? GetEnvDouble("TARGET_LATITUDE")) is { } lat)
        options.TargetLatitude = lat;

    if ((GetEnvDouble("TARGET_LNG") ?? GetEnvDouble("TARGET_LONGITUDE")) is { } lng)
        options.TargetLongitude = lng;

    if ((GetEnvDouble("STOP_RADIUS_METERS") ?? GetEnvDouble("RADIUS")) is { } radius)
        options.StopRadiusMeters = radius;

    if (GetEnvDouble("DIRECT_POINT_RADIUS_METERS") is { } directRadius)
        options.DirectPointRadiusMeters = directRadius;

    if (bool.TryParse(GetEnv("ENABLE_DIRECT_POINT_CHECK"), out bool enableDirect))
        options.EnableDirectPointCheck = enableDirect;

    if ((GetEnvDouble("EXPECTED_AZIMUTH") ?? GetEnvDouble("AZIMUTH")) is { } azimuth)
        options.ExpectedAzimuth = azimuth < 0 ? null : azimuth;

    if ((GetEnvDouble("AZIMUTH_TOLERANCE") ?? GetEnvDouble("AZIMUTH_TOLERANCE_DEGREES")) is { } azTolerance)
        options.AzimuthToleranceDegrees = azTolerance;

    if ((GetEnvInt("POLL_INTERVAL_SECONDS") ?? GetEnvInt("POLL_INTERVAL")) is { } pollSec)
        options.PollIntervalSeconds = pollSec;

    if (GetEnv("LOCAL_LOG_FILE_PATH") is { Length: > 0 } logPath)
        options.LocalLogFilePath = logPath;

    if (GetEnv("GOOGLE_SHEETS_WEBHOOK_URL") is { Length: > 0 } webhookUrl)
        options.GoogleSheetsWebhookUrl = webhookUrl;
});

builder.Services.AddHttpClient<IDozorClient, DozorClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json, text/plain, */*");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    UseCookies = true,
    CookieContainer = new System.Net.CookieContainer()
});

builder.Services.AddHttpClient<IGoogleSheetsWebhookService, GoogleSheetsWebhookService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
});

builder.Services.AddSingleton<IStopDetector, StopDetector>();
builder.Services.AddSingleton<IEventLoggerService, EventLoggerService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

static string? GetEnv(string key) => Environment.GetEnvironmentVariable(key);

static double? GetEnvDouble(string key)
{
    var val = Environment.GetEnvironmentVariable(key);
    return double.TryParse(val, System.Globalization.CultureInfo.InvariantCulture, out double result) ? result : null;
}

static int? GetEnvInt(string key)
{
    var val = Environment.GetEnvironmentVariable(key);
    return int.TryParse(val, out int result) ? result : null;
}
