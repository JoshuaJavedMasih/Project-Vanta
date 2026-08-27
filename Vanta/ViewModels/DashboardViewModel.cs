using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Vanta.Models;

namespace Vanta.ViewModels;

public sealed class DashboardViewModel : INotifyPropertyChanged
{
    private TelemetrySnapshot _snapshot = TelemetrySnapshot.Empty;
    private string _selectedSection = "overview";
    private string _detailTitle = "System overview";
    private string _detailSubtitle = "Real-time telemetry from native Windows sources.";
    private string _detailEyebrow = "LIVE TELEMETRY";
    private string _detailHeroValue = "Scanning";
    private string _detailHeroLabel = "CURRENT STATUS";

    public DashboardViewModel(bool isSimulated)
    {
        IsSimulated = isSimulated;
        SourceLabel = isSimulated ? "SIMULATED PROVIDER" : "WINDOWS NATIVE / LIVE";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsSimulated { get; }
    public string SourceLabel { get; }
    public ObservableCollection<ProcessDisplayItem> TopProcesses { get; } = new();
    public ObservableCollection<DriveDisplayItem> Drives { get; } = new();
    public ObservableCollection<MetricItem> DetailMetrics { get; } = new();

    public TelemetrySnapshot Snapshot
    {
        get => _snapshot;
        private set
        {
            _snapshot = value;
            OnPropertyChanged();
            NotifySnapshotProperties();
        }
    }

    public string Greeting => DateTime.Now.Hour switch
    {
        < 12 => "Good morning",
        < 18 => "Good afternoon",
        _ => "Good evening"
    };

    public string CpuUsage => $"{Snapshot.CpuUsagePercent:0}%";
    public double CpuUsageValue => Snapshot.CpuUsagePercent;
    public string CpuName => Snapshot.CpuName;
    public string CpuClockShort => Snapshot.CpuClockMhz is int clock ? $"{clock / 1000d:0.0} GHz" : "Live clock";
    public string CpuLogicalShort => $"{Snapshot.LogicalProcessorCount} logical processors";
    public string CpuSecondary => Snapshot.CpuClockMhz is int clock ? $"{clock / 1000d:0.0} GHz  ·  {Snapshot.LogicalProcessorCount} logical processors" : $"{Snapshot.LogicalProcessorCount} logical processors";
    public string CpuTemperature => FormatTemperature(Snapshot.CpuTemperatureCelsius);
    public string GpuUsage => Snapshot.GpuUsagePercent is double value ? $"{value:0}%" : "—";
    public double GpuUsageValue => Snapshot.GpuUsagePercent ?? 0;
    public string GpuName => Snapshot.GpuName;
    public string GpuSecondary => Snapshot.GpuTemperatureCelsius is double temperature ? $"{temperature:0}°C  ·  Sensor active" : "Utilization sensor not exposed";
    public string GpuTemperature => FormatTemperature(Snapshot.GpuTemperatureCelsius);
    public double MemoryUsageValue => Snapshot.TotalMemoryBytes <= 0 ? 0 : Snapshot.UsedMemoryBytes * 100d / Snapshot.TotalMemoryBytes;
    public string MemoryUsage => $"{MemoryUsageValue:0}%";
    public string MemoryCapacity => Snapshot.TotalMemoryBytes <= 0 ? "Detecting memory…" : $"{FormatBytes(Snapshot.UsedMemoryBytes)} of {FormatBytes(Snapshot.TotalMemoryBytes)}";
    public string MemoryAvailable => Snapshot.TotalMemoryBytes <= 0 ? "—" : FormatBytes(Snapshot.TotalMemoryBytes - Snapshot.UsedMemoryBytes);
    public double StorageUsageValue => Snapshot.Drives.FirstOrDefault()?.UsedPercent ?? 0;
    public string StorageUsage => Snapshot.Drives.FirstOrDefault() is { } drive ? $"{drive.UsedPercent:0}%" : "—";
    public string StorageName => Snapshot.Drives.FirstOrDefault() is { } drive ? $"{drive.Name}  ·  {drive.Label}" : "No ready fixed drive";
    public string StorageFree => Snapshot.Drives.FirstOrDefault() is { } drive ? FormatBytes(drive.FreeBytes) : "—";
    public string NetworkDownload => $"{Snapshot.Network.DownloadMbps:0.0} Mbps";
    public string NetworkUpload => $"{Snapshot.Network.UploadMbps:0.0} Mbps";
    public string NetworkName => Snapshot.Network.AdapterName;
    public string Uptime => FormatUptime(Snapshot.Uptime);
    public string SensorCount => Snapshot.AvailableSensorCount.ToString();
    public string AlertCount => Snapshot.HealthStatus == "Healthy" ? "0" : "1";
    public string HealthStatus => Snapshot.HealthStatus;
    public string SystemHealthHeadline => $"System {Snapshot.HealthStatus}";
    public string SystemHealthStatusLine => $"SYSTEM {Snapshot.HealthStatus.ToUpperInvariant()}";
    public string HealthDetail => Snapshot.HealthDetail;
    public string UpdatedAt => Snapshot.Timestamp == TelemetrySnapshot.Empty.Timestamp ? "Waiting for first sample" : $"Updated {Snapshot.Timestamp:HH:mm:ss}";
    public string StatusLine => $"{Snapshot.AvailableSensorCount} signals  ·  {Snapshot.Processes.Count} active samples  ·  1 sec interval";
    public string SensorStatus => $"Sensors: {Snapshot.AvailableSensorCount}";
    public string AlertStatus => $"Alerts: {AlertCount}";
    public string ConnectionStatus => Snapshot.Network.IsConnected ? "Connected" : "Offline";

    public string DetailTitle
    {
        get => _detailTitle;
        private set => SetField(ref _detailTitle, value);
    }

    public string DetailSubtitle
    {
        get => _detailSubtitle;
        private set => SetField(ref _detailSubtitle, value);
    }

    public string DetailEyebrow
    {
        get => _detailEyebrow;
        private set => SetField(ref _detailEyebrow, value);
    }

    public string DetailHeroValue
    {
        get => _detailHeroValue;
        private set => SetField(ref _detailHeroValue, value);
    }

    public string DetailHeroLabel
    {
        get => _detailHeroLabel;
        private set => SetField(ref _detailHeroLabel, value);
    }

    public void ApplySnapshot(TelemetrySnapshot snapshot)
    {
        Snapshot = snapshot;
        ReplaceCollection(TopProcesses, snapshot.Processes.Select(item => new ProcessDisplayItem(
            item.Name,
            item.ProcessId.ToString(),
            $"{item.CpuPercent:0.0}%",
            FormatBytes(item.WorkingSetBytes),
            item.ThreadCount.ToString())));
        ReplaceCollection(Drives, snapshot.Drives.Select(item => new DriveDisplayItem(
            item.Name,
            item.Label,
            item.Format,
            $"{FormatBytes(item.UsedBytes)} / {FormatBytes(item.TotalBytes)}",
            item.UsedPercent,
            $"{FormatBytes(item.FreeBytes)} free")));
        RefreshDetail();
    }

    public void SelectSection(string section)
    {
        _selectedSection = section;
        RefreshDetail();
    }

    private void RefreshDetail()
    {
        var metrics = new List<MetricItem>();
        switch (_selectedSection)
        {
            case "cpu":
                DetailTitle = "CPU intelligence";
                DetailSubtitle = Snapshot.CpuName;
                DetailEyebrow = "PROCESSOR / LIVE";
                DetailHeroValue = CpuUsage;
                DetailHeroLabel = "TOTAL UTILIZATION";
                metrics.Add(new("Current clock", Snapshot.CpuClockMhz is int clock ? $"{clock / 1000d:0.00} GHz" : "Unavailable", "Registry-reported operating clock"));
                metrics.Add(new("Logical processors", Snapshot.LogicalProcessorCount.ToString(), "Windows scheduling units"));
                metrics.Add(new("Package temperature", CpuTemperature, Snapshot.CpuTemperatureCelsius is null ? "Requires a supported sensor provider" : "Live sensor"));
                metrics.Add(new("Sampling", "1 second", "Collection and interface cadence"));
                break;
            case "gpu":
                DetailTitle = "GPU intelligence";
                DetailSubtitle = Snapshot.GpuName;
                DetailEyebrow = "GRAPHICS / ADAPTER";
                DetailHeroValue = GpuUsage;
                DetailHeroLabel = "GPU UTILIZATION";
                metrics.Add(new("Dedicated memory", Snapshot.GpuMemoryBytes is long memory ? FormatBytes(memory) : "Unavailable", "Adapter registry capability"));
                metrics.Add(new("Temperature", GpuTemperature, "Shown only when a supported source exists"));
                metrics.Add(new("Utilization", GpuUsage, Snapshot.GpuUsagePercent is null ? "Windows did not expose a reliable counter" : "Live sensor"));
                metrics.Add(new("Privacy", "Protected", "No adapter serial identifiers collected"));
                break;
            case "memory":
                DetailTitle = "Memory intelligence";
                DetailSubtitle = "Physical memory pressure and availability";
                DetailEyebrow = "MEMORY / LIVE";
                DetailHeroValue = MemoryUsage;
                DetailHeroLabel = "PHYSICAL MEMORY USED";
                metrics.Add(new("Installed", FormatBytes(Snapshot.TotalMemoryBytes), "Physical RAM visible to Windows"));
                metrics.Add(new("In use", FormatBytes(Snapshot.UsedMemoryBytes), "Current committed physical usage"));
                metrics.Add(new("Available", MemoryAvailable, "Immediately available to applications"));
                metrics.Add(new("Pressure", MemoryUsageValue >= 85 ? "Elevated" : "Normal", "Attention threshold: 85%"));
                break;
            case "storage":
                var drive = Snapshot.Drives.FirstOrDefault();
                DetailTitle = "Storage intelligence";
                DetailSubtitle = drive is null ? "No ready fixed drive detected" : $"{drive.Name}  ·  {drive.Label}";
                DetailEyebrow = "STORAGE / CAPACITY";
                DetailHeroValue = StorageUsage;
                DetailHeroLabel = "PRIMARY DRIVE USED";
                metrics.Add(new("Capacity", drive is null ? "Unavailable" : FormatBytes(drive.TotalBytes), "Usable file-system capacity"));
                metrics.Add(new("Free", drive is null ? "Unavailable" : FormatBytes(drive.FreeBytes), "Space currently available"));
                metrics.Add(new("File system", drive?.Format ?? "Unknown", "Reported by Windows"));
                metrics.Add(new("Health evidence", "Capacity only", "SMART/NVMe provider planned; no failure claim made"));
                break;
            case "network":
                DetailTitle = "Network intelligence";
                DetailSubtitle = Snapshot.Network.AdapterName;
                DetailEyebrow = "NETWORK / LIVE";
                DetailHeroValue = NetworkDownload;
                DetailHeroLabel = "CURRENT DOWNLOAD";
                metrics.Add(new("Upload", NetworkUpload, "Current interface throughput"));
                metrics.Add(new("Link speed", FormatBits(Snapshot.Network.LinkSpeedBitsPerSecond), "Negotiated adapter speed"));
                metrics.Add(new("Interface", Snapshot.Network.InterfaceType, "Active Windows network adapter"));
                metrics.Add(new("Connection", Snapshot.Network.IsConnected ? "Connected" : "Offline", "No packet interception performed"));
                break;
            case "processes":
                var top = Snapshot.Processes.FirstOrDefault();
                DetailTitle = "Process activity";
                DetailSubtitle = "A least-privilege view of current application pressure";
                DetailEyebrow = "PROCESSES / LIVE";
                DetailHeroValue = Snapshot.Processes.Count.ToString();
                DetailHeroLabel = "TOP PROCESSES SAMPLED";
                metrics.Add(new("Highest activity", top?.Name ?? "Unavailable", "Ranked by current CPU then memory"));
                metrics.Add(new("Top CPU", top is null ? "—" : $"{top.CpuPercent:0.0}%", "Normalized across logical processors"));
                metrics.Add(new("Top memory", top is null ? "—" : FormatBytes(top.WorkingSetBytes), "Current working set"));
                metrics.Add(new("Safety", "Read only", "No termination action is enabled in Phase 1"));
                break;
            case "sensors":
                DetailTitle = "Sensor center";
                DetailSubtitle = "Every signal is labeled by availability and source";
                DetailEyebrow = "SENSORS / INVENTORY";
                DetailHeroValue = SensorCount;
                DetailHeroLabel = "AVAILABLE SIGNALS";
                metrics.Add(new("CPU utilization", "Available", "Native GetSystemTimes telemetry"));
                metrics.Add(new("Memory pressure", "Available", "Native GlobalMemoryStatusEx telemetry"));
                metrics.Add(new("Network throughput", Snapshot.Network.IsConnected ? "Available" : "Unavailable", "Active adapter byte counters"));
                metrics.Add(new("Temperature sensors", Snapshot.CpuTemperatureCelsius is null ? "Unavailable" : "Available", "Optional hardware provider boundary ready"));
                break;
            case "health":
                DetailTitle = "System health";
                DetailSubtitle = "Conservative status from clearly measurable thresholds";
                DetailEyebrow = "HEALTH / EXPLAINABLE";
                DetailHeroValue = HealthStatus;
                DetailHeroLabel = "CURRENT ASSESSMENT";
                metrics.Add(new("Memory threshold", "85% / 95%", "Attention / Critical"));
                metrics.Add(new("Disk threshold", "90% / 98%", "Attention / Critical capacity"));
                metrics.Add(new("Temperature", "Not scored", "No reliable source currently available"));
                metrics.Add(new("Confidence", "Measured", "No fabricated percentage"));
                break;
            case "alerts":
                SetFoundation("Alerts", "Threshold-based warnings with explainable evidence", AlertCount, "ACTIVE ALERTS", "Rule engine foundation", "No alert is created from unavailable data");
                return;
            case "services":
                SetFoundation("Services", "Windows service inspection with elevation only when required", "Read-only", "PHASE 2 FOUNDATION", "Least privilege", "Mutation actions remain intentionally disabled");
                return;
            case "startup":
                SetFoundation("Startup", "Login-impact inspection without deleting startup entries", "Safe", "PHASE 2 FOUNDATION", "Planned sources", "Registry, startup folders and scheduled tasks");
                return;
            case "history":
                SetFoundation("History", "Configurable local retention for rolling performance data", "Off", "DEFAULT RETENTION", "Privacy first", "History storage is disabled until explicitly enabled");
                return;
            case "reports":
                SetFoundation("Reports", "Shareable diagnostics with private identifiers hidden by default", "JSON", "AVAILABLE EXPORT", "Privacy defaults", "Username, hostname, IP and serials are omitted");
                return;
            case "settings":
                SetFoundation("Settings", "Monitoring, appearance, privacy and retention controls", "Dark", "SIGNATURE THEME", "Polling interval", "1 second; service and UI work are decoupled");
                return;
            case "about":
                DetailTitle = "About Vanta";
                DetailSubtitle = "Premium Windows system monitoring by Merhatta Softwares";
                DetailEyebrow = "VANTA / PRODUCT";
                DetailHeroValue = "1.0.0";
                DetailHeroLabel = "CURRENT VERSION";
                ReplaceCollection(DetailMetrics, new[]
                {
                    new MetricItem("Made by", "Merhatta Softwares", "Product design, engineering, and original Vanta branding"),
                    new MetricItem("Copyright", "© 2026", "Merhatta Softwares. All rights reserved."),
                    new MetricItem("Platform", ".NET 8 + WinUI 3", "Windows App SDK 1.8 stable channel"),
                    new MetricItem("Privacy", "Local by default", "No personal analytics collection")
                });
                return;
            default:
                return;
        }

        ReplaceCollection(DetailMetrics, metrics);
    }

    private void SetFoundation(string title, string subtitle, string hero, string heroLabel, string metricLabel, string metricDetail)
    {
        DetailTitle = title;
        DetailSubtitle = subtitle;
        DetailEyebrow = "VANTA / PLATFORM";
        DetailHeroValue = hero;
        DetailHeroLabel = heroLabel;
        ReplaceCollection(DetailMetrics, new[]
        {
            new MetricItem(metricLabel, "Ready", metricDetail),
            new MetricItem("Privilege model", "Standard user", "Elevation is requested only for protected actions"),
            new MetricItem("Local telemetry", "Private", "No personal analytics collection"),
            new MetricItem("Architecture", "Modular", "Provider → service → model → view model → UI")
        });
    }

    private void NotifySnapshotProperties()
    {
        string[] names =
        {
            nameof(Greeting), nameof(CpuUsage), nameof(CpuUsageValue), nameof(CpuName), nameof(CpuClockShort), nameof(CpuLogicalShort), nameof(CpuSecondary), nameof(CpuTemperature),
            nameof(GpuUsage), nameof(GpuUsageValue), nameof(GpuName), nameof(GpuSecondary), nameof(GpuTemperature),
            nameof(MemoryUsage), nameof(MemoryUsageValue), nameof(MemoryCapacity), nameof(MemoryAvailable),
            nameof(StorageUsage), nameof(StorageUsageValue), nameof(StorageName), nameof(StorageFree), nameof(NetworkDownload), nameof(NetworkUpload),
            nameof(NetworkName), nameof(Uptime), nameof(SensorCount), nameof(AlertCount), nameof(HealthStatus), nameof(SystemHealthHeadline),
            nameof(SystemHealthStatusLine), nameof(HealthDetail),
            nameof(UpdatedAt), nameof(StatusLine), nameof(SensorStatus), nameof(AlertStatus), nameof(ConnectionStatus)
        };
        foreach (var name in names)
        {
            OnPropertyChanged(name);
        }
    }

    private static void ReplaceCollection<T>(ObservableCollection<T> target, IEnumerable<T> source)
    {
        target.Clear();
        foreach (var item in source)
        {
            target.Add(item);
        }
    }

    public static string FormatBytes(long bytes)
    {
        if (bytes <= 0)
        {
            return "0 GB";
        }

        string[] units = { "B", "KB", "MB", "GB", "TB", "PB" };
        var value = (double)bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{value:0.#} {units[unit]}";
    }

    private static string FormatBits(long bitsPerSecond) => bitsPerSecond <= 0 ? "Unavailable" : $"{bitsPerSecond / 1_000_000d:0.#} Mbps";
    private static string FormatTemperature(double? temperature) => temperature is double value ? $"{value:0}°C" : "Unavailable";
    private static string FormatUptime(TimeSpan uptime) => uptime.TotalDays >= 1 ? $"{(int)uptime.TotalDays}d {uptime.Hours}h" : $"{uptime.Hours}h {uptime.Minutes}m";

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
