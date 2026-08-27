namespace Vanta.Models;

public sealed record TelemetrySnapshot(
    DateTimeOffset Timestamp,
    string CpuName,
    double CpuUsagePercent,
    int LogicalProcessorCount,
    int? CpuClockMhz,
    double? CpuTemperatureCelsius,
    string GpuName,
    double? GpuUsagePercent,
    double? GpuTemperatureCelsius,
    long? GpuMemoryBytes,
    long TotalMemoryBytes,
    long UsedMemoryBytes,
    IReadOnlyList<DriveMetric> Drives,
    NetworkMetric Network,
    IReadOnlyList<ProcessMetric> Processes,
    TimeSpan Uptime,
    int AvailableSensorCount,
    string HealthStatus,
    string HealthDetail)
{
    public static TelemetrySnapshot Empty { get; } = new(
        DateTimeOffset.Now,
        "Detecting processor…",
        0,
        Environment.ProcessorCount,
        null,
        null,
        "Detecting graphics adapter…",
        null,
        null,
        null,
        0,
        0,
        Array.Empty<DriveMetric>(),
        new NetworkMetric("Detecting network…", "Unknown", 0, 0, 0, false),
        Array.Empty<ProcessMetric>(),
        TimeSpan.Zero,
        0,
        "Scanning",
        "Vanta is detecting available Windows telemetry sources.");
}
