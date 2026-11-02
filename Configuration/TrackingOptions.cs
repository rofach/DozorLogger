namespace DozorLogger.Configuration;

public class TrackingOptions
{
    public const string SectionName = "Tracking";

    public string DozorUrl { get; set; } = "https://city.dozor.tech/data?t=2&p={0}";
    public int RouteId { get; set; } = 2381;
    public double TargetLatitude { get; set; } = 49.4307129;
    public double TargetLongitude { get; set; } = 32.0694703;
    public double StopRadiusMeters { get; set; } = 50.0;
    public double DirectPointRadiusMeters { get; set; } = 30.0;
    public bool EnableDirectPointCheck { get; set; } = true;
    public double? ExpectedAzimuth { get; set; } = 130.0;
    public double AzimuthToleranceDegrees { get; set; } = 50.0;
    public int PollIntervalSeconds { get; set; } = 5;
    public string LocalLogFilePath { get; set; } = "logs/bus_stop_intervals.csv";
    public string? GoogleSheetsWebhookUrl { get; set; }

    public string GetEffectiveDozorUrl()
    {
        if (DozorUrl.Contains("{0}")) return string.Format(DozorUrl, RouteId);
        if (DozorUrl.Contains("{RouteId}", StringComparison.OrdinalIgnoreCase))
            return DozorUrl.Replace("{RouteId}", RouteId.ToString(), StringComparison.OrdinalIgnoreCase);
        return DozorUrl;
    }
}
