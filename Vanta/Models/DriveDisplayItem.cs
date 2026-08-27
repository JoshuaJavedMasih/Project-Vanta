namespace Vanta.Models;

public sealed record DriveDisplayItem(
    string Name,
    string Label,
    string Format,
    string Usage,
    double UsedPercent,
    string FreeSpace);
