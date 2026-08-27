using System.Text.Json;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Polyline = Microsoft.UI.Xaml.Shapes.Polyline;
using Vanta.Models;
using Vanta.Services;
using Vanta.ViewModels;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Vanta.Diagnostics;

namespace Vanta;

public sealed partial class MainPage : Page
{
    private const int MaximumTrendSamples = 60;
    private readonly MonitoringService _monitoringService;
    private readonly Queue<double> _cpuTrend = new();
    private readonly Queue<double> _gpuTrend = new();
    private readonly Queue<double> _memoryTrend = new();
    private readonly Queue<double> _storageTrend = new();
    private readonly Queue<double> _downloadTrend = new();
    private readonly Queue<double> _uploadTrend = new();
    private bool _gpuTrendAvailable;

    public MainPage()
    {
        StartupTrace.Mark("MainPage: begin");
        var isSimulated = string.Equals(Environment.GetEnvironmentVariable("VANTA_DEMO"), "1", StringComparison.Ordinal);
        ViewModel = new DashboardViewModel(isSimulated);
        ITelemetryProvider provider = isSimulated ? new SimulatedTelemetryProvider() : new WindowsTelemetryProvider();
        _monitoringService = new MonitoringService(provider);
        StartupTrace.Mark("MainPage: telemetry provider ready");

        InitializeComponent();
        StartupTrace.Mark("MainPage: XAML initialized");
        Loaded += MainPage_Loaded;
        Unloaded += MainPage_Unloaded;
    }

    public DashboardViewModel ViewModel { get; }

    private void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        StartupTrace.Mark("MainPage: Loaded event");
        _monitoringService.SnapshotAvailable += MonitoringService_SnapshotAvailable;
        _monitoringService.Start();

#if DEBUG
        var capturePath = Environment.GetEnvironmentVariable("VANTA_CAPTURE");
        if (!string.IsNullOrWhiteSpace(capturePath))
        {
            _ = CaptureDebugPreviewAfterDelayAsync(capturePath);
        }
#endif
    }

    private async void MainPage_Unloaded(object sender, RoutedEventArgs e)
    {
        _monitoringService.SnapshotAvailable -= MonitoringService_SnapshotAvailable;
        await _monitoringService.StopAsync();
    }

    private void MonitoringService_SnapshotAvailable(object? sender, TelemetrySnapshot snapshot)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            ViewModel.ApplySnapshot(snapshot);
            AppendTrend(snapshot);
        });
    }

    private void AppendTrend(TelemetrySnapshot snapshot)
    {
        AddSample(_cpuTrend, snapshot.CpuUsagePercent);
        AddSample(_memoryTrend, snapshot.TotalMemoryBytes <= 0 ? 0 : snapshot.UsedMemoryBytes * 100d / snapshot.TotalMemoryBytes);
        AddSample(_storageTrend, snapshot.Drives.FirstOrDefault()?.UsedPercent ?? 0);
        AddRawSample(_downloadTrend, snapshot.Network.DownloadMbps);
        AddRawSample(_uploadTrend, snapshot.Network.UploadMbps);
        if (snapshot.GpuUsagePercent is double gpu)
        {
            _gpuTrendAvailable = true;
            AddSample(_gpuTrend, gpu);
        }

        RenderTrends();
    }

    private static void AddSample(Queue<double> samples, double value)
    {
        samples.Enqueue(Math.Clamp(value, 0, 100));
        while (samples.Count > MaximumTrendSamples)
        {
            samples.Dequeue();
        }
    }

    private static void AddRawSample(Queue<double> samples, double value)
    {
        samples.Enqueue(Math.Max(0, value));
        while (samples.Count > MaximumTrendSamples)
        {
            samples.Dequeue();
        }
    }

    private void TrendCanvas_SizeChanged(object sender, SizeChangedEventArgs e) => RenderTrends();

    private void RenderTrends()
    {
        RenderPercentTrend(CpuGaugeTrendCanvas, CpuGaugeTrendLine, _cpuTrend);
        RenderPercentTrend(CpuSensorTrendCanvas, CpuSensorTrendLine, _cpuTrend);
        RenderPercentTrend(MemoryGaugeTrendCanvas, MemoryGaugeTrendLine, _memoryTrend);
        RenderPercentTrend(MemorySensorTrendCanvas, MemorySensorTrendLine, _memoryTrend);
        RenderPercentTrend(StorageSensorTrendCanvas, StorageSensorTrendLine, _storageTrend);
        RenderPercentTrend(DiskTrendCanvas, StorageTrendLine, _storageTrend);

        if (_gpuTrendAvailable)
        {
            RenderPercentTrend(GpuGaugeTrendCanvas, GpuGaugeTrendLine, _gpuTrend);
            RenderPercentTrend(GpuSensorTrendCanvas, GpuSensorTrendLine, _gpuTrend);
        }
        else
        {
            GpuGaugeTrendLine.Points.Clear();
            GpuSensorTrendLine.Points.Clear();
        }

        var networkMaximum = Math.Max(1, _downloadTrend.Concat(_uploadTrend).DefaultIfEmpty(0).Max());
        SetTrendPoints(NetworkDownloadTrendLine.Points, _downloadTrend, NetworkTrendCanvas.ActualWidth, NetworkTrendCanvas.ActualHeight, networkMaximum);
        SetTrendPoints(NetworkUploadTrendLine.Points, _uploadTrend, NetworkTrendCanvas.ActualWidth, NetworkTrendCanvas.ActualHeight, networkMaximum);
    }

    private static void RenderPercentTrend(Canvas canvas, Polyline line, IReadOnlyCollection<double> samples) =>
        SetTrendPoints(line.Points, samples, canvas.ActualWidth, canvas.ActualHeight, 100);

    private static void SetTrendPoints(PointCollection points, IReadOnlyCollection<double> samples, double width, double height, double maximum)
    {
        points.Clear();
        if (samples.Count == 0 || width <= 0 || height <= 0)
        {
            return;
        }

        var values = samples.ToArray();
        var denominator = Math.Max(1, values.Length - 1);
        for (var index = 0; index < values.Length; index++)
        {
            var x = index / (double)denominator * width;
            var y = (1 - Math.Clamp(values[index] / maximum, 0, 1)) * Math.Max(1, height - 2) + 1;
            points.Add(new Point(x, y));
        }
    }

    private void RootNavigation_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItemContainer?.Tag is not string tag)
        {
            return;
        }

        var isOverview = tag == "overview";
        OverviewView.Visibility = isOverview ? Visibility.Visible : Visibility.Collapsed;
        DetailView.Visibility = isOverview ? Visibility.Collapsed : Visibility.Visible;
        if (!isOverview)
        {
            ViewModel.SelectSection(tag);
        }
    }

    private void SelectNavigationTag(string tag)
    {
        foreach (var item in RootNavigation.MenuItems.Concat(RootNavigation.FooterMenuItems).OfType<NavigationViewItem>())
        {
            if (string.Equals(item.Tag as string, tag, StringComparison.Ordinal))
            {
                RootNavigation.SelectedItem = item;
                return;
            }
        }
    }

    private void ViewProcesses_Click(object sender, RoutedEventArgs e) => SelectNavigationTag("processes");

    private async void ExportSnapshot_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileSavePicker
        {
            SuggestedFileName = $"Vanta-snapshot-{DateTime.Now:yyyyMMdd-HHmmss}",
            SuggestedStartLocation = PickerLocationId.Downloads
        };
        picker.FileTypeChoices.Add("JSON diagnostic snapshot", new List<string> { ".json" });

        if (App.MainWindow is null)
        {
            return;
        }

        var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, windowHandle);
        var file = await picker.PickSaveFileAsync();
        if (file is null)
        {
            return;
        }

        var json = JsonSerializer.Serialize(ViewModel.Snapshot, new JsonSerializerOptions { WriteIndented = true });
        await FileIO.WriteTextAsync(file, json);

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Snapshot exported",
            Content = "The JSON snapshot was saved with username, computer name, IP addresses, and hardware serial numbers omitted.",
            CloseButtonText = "Done",
            DefaultButton = ContentDialogButton.Close
        };
        await dialog.ShowAsync();
    }

#if DEBUG
    private async Task CaptureDebugPreviewAfterDelayAsync(string capturePath)
    {
        var captureSection = Environment.GetEnvironmentVariable("VANTA_CAPTURE_SECTION");
        if (!string.IsNullOrWhiteSpace(captureSection))
        {
            SelectNavigationTag(captureSection);
        }

        await Task.Delay(5500);
        await CaptureDebugPreviewAsync(capturePath);
    }

    private async Task CaptureDebugPreviewAsync(string capturePath)
    {
        try
        {
            var fullPath = Path.GetFullPath(capturePath);
            var folderPath = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return;
            }

            var renderTarget = new RenderTargetBitmap();
            await renderTarget.RenderAsync(this);
            var pixels = await renderTarget.GetPixelsAsync();
            var folder = await StorageFolder.GetFolderFromPathAsync(folderPath);
            var file = await folder.CreateFileAsync(Path.GetFileName(fullPath), CreationCollisionOption.ReplaceExisting);
            await using var stream = await file.OpenStreamForWriteAsync();
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream.AsRandomAccessStream());
            encoder.SetPixelData(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Premultiplied,
                (uint)renderTarget.PixelWidth,
                (uint)renderTarget.PixelHeight,
                96,
                96,
                pixels.ToArray());
            await encoder.FlushAsync();
            StartupTrace.Mark($"Debug preview captured: {renderTarget.PixelWidth}x{renderTarget.PixelHeight}");
        }
        catch (Exception error)
        {
            StartupTrace.Mark($"Debug preview failed: {error}");
        }
    }
#endif
}
