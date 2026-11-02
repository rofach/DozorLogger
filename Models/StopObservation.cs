using CsvHelper.Configuration;

namespace DozorLogger.Models;

public class StopObservation
{
    public int Index { get; set; }
    public DateTime LocalTime { get; set; }
    public double? IntervalMinutes { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public int VehicleId { get; set; }
    public double DistanceMeters { get; set; }
    public double SpeedKmH { get; set; }
    public double Azimuth { get; set; }
}

public sealed class StopObservationMap : ClassMap<StopObservation>
{
    public StopObservationMap()
    {
        Map(m => m.Index).Name("№").Index(0);
        Map(m => m.LocalTime).Name("Час зупинки").Index(1).TypeConverterOption.Format("yyyy-MM-dd HH:mm:ss");
        Map(m => m.IntervalMinutes).Name("Інтервал").Index(2).TypeConverterOption.Format("0.0").TypeConverterOption.NullValues("-");
        Map(m => m.LicensePlate).Name("Номер автобуса").Index(3);
        Map(m => m.VehicleId).Name("ID трекера").Index(4);
        Map(m => m.DistanceMeters).Name("Дистанція").Index(5).TypeConverterOption.Format("0.0");
        Map(m => m.SpeedKmH).Name("Швидкість").Index(6).TypeConverterOption.Format("0.0");
        Map(m => m.Azimuth).Ignore();
    }
}
