namespace Vanta.Models;

public sealed record ProcessDisplayItem(
    string Name,
    string ProcessId,
    string Cpu,
    string Memory,
    string Threads);
