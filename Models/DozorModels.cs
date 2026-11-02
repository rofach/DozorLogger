using System.Text.Json.Serialization;

namespace DozorLogger.Models;

public class DozorResponse
{
    [JsonPropertyName("data")]
    public List<DozorRouteData>? Data { get; set; }
}

public class DozorRouteData
{
    [JsonPropertyName("rId")]
    public int RouteId { get; set; }

    [JsonPropertyName("dvs")]
    public List<DozorBus>? Buses { get; set; }
}

public class DozorBus
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("loc")]
    public DozorLocation? Location { get; set; }

    [JsonPropertyName("spd")]
    public double Speed { get; set; }

    [JsonPropertyName("azi")]
    public double Azimuth { get; set; }

    [JsonPropertyName("gNb")]
    public string? LicensePlate { get; set; }
}

public class DozorLocation
{
    [JsonPropertyName("lat")]
    public double Latitude { get; set; }

    [JsonPropertyName("lng")]
    public double Longitude { get; set; }
}

