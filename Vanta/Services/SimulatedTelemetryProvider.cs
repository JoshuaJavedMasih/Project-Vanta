using Vanta.Models;

namespace Vanta.Services;

public sealed class SimulatedTelemetryProvider : ITelemetryProvider
{
    private readonly Random _random = new(42);
    private double _cpu = 34;
    private double _gpu = 61;
    private double _memory = 58;

    public TelemetrySnapshot Capture()
    {
        _cpu = Next(_cpu, 8, 92, 12);
        _gpu = Next(_gpu, 12, 98, 9);
        _memory = Next(_memory, 42, 82, 2);
        const long totalMemory = 32L * 1024 * 1024 * 1024;

        return new TelemetrySnapshot(
            DateTimeOffset.Now,
            "Intel Core i7-14700K",
            _cpu,
            28,
            4100,
            58 + (_cpu - 34) * 0.18,
            "NVIDIA GeForce RTX 5070",
            _gpu,
            64 + (_gpu - 61) * 0.12,
            12L * 1024 * 1024 * 1024,
            totalMemory,
            (long)(totalMemory * _memory / 100),
            new[] { new DriveMetric("C:", "Samsung 990 PRO", "NTFS", 2_000_398_934_016, 1_210_000_000_000) },
            new NetworkMetric("Intel Wi-Fi 7 BE200", "Wireless", 252.4, 38.2, 2_400_000_000, true),
            new[]
            {
                new ProcessMetric("Vanta", 10824, 1.2, 188_000_000, 34),
                new ProcessMetric("msedge", 4212, 4.8, 1_420_000_000, 96),
                new ProcessMetric("devenv", 7716, 3.1, 980_000_000, 82),
                new ProcessMetric("explorer", 2156, 0.6, 246_000_000, 51)
            },
            TimeSpan.FromDays(2) + TimeSpan.FromHours(14),
            34,
            "Healthy",
            "No critical resource-pressure condition is currently visible.");
    }

    private double Next(double current, double minimum, double maximum, double movement) =>
        Math.Clamp(current + (_random.NextDouble() - 0.5) * movement, minimum, maximum);
}
