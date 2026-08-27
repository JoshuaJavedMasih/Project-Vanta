namespace Vanta.Models;

public sealed record DriveMetric(
    string Name,
    string Label,
    string Format,
    long TotalBytes,
    long UsedBytes)
{
    public double UsedPercent => TotalBytes <= 0 ? 0 : UsedBytes * 100d / TotalBytes;
    public long FreeBytes => Math.Max(0, TotalBytes - UsedBytes);
}
