namespace Vanta.Models;

public sealed record ProcessMetric(
    string Name,
    int ProcessId,
    double CpuPercent,
    long WorkingSetBytes,
    int ThreadCount);
