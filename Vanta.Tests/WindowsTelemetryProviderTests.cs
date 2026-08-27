using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vanta.Services;

namespace Vanta.Tests;

[TestClass]
public sealed class WindowsTelemetryProviderTests
{
    [TestMethod]
    public void Capture_ReturnsValidNativeResourceValues()
    {
        var provider = new WindowsTelemetryProvider();
        Thread.Sleep(50);
        var snapshot = provider.Capture();

        Assert.IsFalse(string.IsNullOrWhiteSpace(snapshot.CpuName));
        Assert.IsTrue(snapshot.CpuUsagePercent is >= 0 and <= 100);
        Assert.IsGreaterThan(0, snapshot.LogicalProcessorCount);
        Assert.IsGreaterThan(0L, snapshot.TotalMemoryBytes);
        Assert.IsTrue(snapshot.UsedMemoryBytes <= snapshot.TotalMemoryBytes);
        Assert.IsNotNull(snapshot.Drives);
        Assert.IsNotNull(snapshot.Processes);
    }
}
