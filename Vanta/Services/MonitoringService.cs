using Vanta.Models;

namespace Vanta.Services;

public sealed class MonitoringService : IDisposable
{
    private readonly ITelemetryProvider _provider;
    private readonly TimeSpan _interval;
    private CancellationTokenSource? _cancellation;
    private Task? _monitoringTask;

    public MonitoringService(ITelemetryProvider provider, TimeSpan? interval = null)
    {
        _provider = provider;
        _interval = interval ?? TimeSpan.FromSeconds(1);
    }

    public event EventHandler<TelemetrySnapshot>? SnapshotAvailable;

    public void Start()
    {
        if (_monitoringTask is { IsCompleted: false })
        {
            return;
        }

        _cancellation = new CancellationTokenSource();
        _monitoringTask = Task.Run(() => RunAsync(_cancellation.Token));
    }

    public async Task StopAsync()
    {
        if (_cancellation is null || _monitoringTask is null)
        {
            return;
        }

        await _cancellation.CancelAsync();
        try
        {
            await _monitoringTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _cancellation.Dispose();
            _cancellation = null;
            _monitoringTask = null;
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(_interval);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                SnapshotAvailable?.Invoke(this, _provider.Capture());
            }
            catch
            {
                // A failed sensor must never take down the monitoring loop.
            }

            await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        _cancellation?.Cancel();
        _cancellation?.Dispose();
    }
}
