using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vanta.Models;
using Vanta.Services;

namespace Vanta.Tests;

[TestClass]
public sealed class MonitoringServiceTests
{
    [TestMethod]
    public async Task Start_PublishesSnapshotWithoutBlockingCaller()
    {
        using var service = new MonitoringService(new SimulatedTelemetryProvider(), TimeSpan.FromMilliseconds(20));
        var received = new TaskCompletionSource<TelemetrySnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        service.SnapshotAvailable += (_, snapshot) => received.TrySetResult(snapshot);

        service.Start();
        var snapshot = await received.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await service.StopAsync();

        Assert.IsNotNull(snapshot);
        Assert.IsGreaterThan(0, snapshot.AvailableSensorCount);
    }
}
