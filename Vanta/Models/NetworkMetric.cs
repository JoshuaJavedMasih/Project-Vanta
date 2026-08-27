namespace Vanta.Models;

public sealed record NetworkMetric(
    string AdapterName,
    string InterfaceType,
    double DownloadMbps,
    double UploadMbps,
    long LinkSpeedBitsPerSecond,
    bool IsConnected);
