using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vanta.Services;

namespace Vanta.Tests;

[TestClass]
public sealed class SimulatedTelemetryProviderTests
{
    [TestMethod]
    public void Capture_ReturnsBoundedCoherentSnapshot()
    {
        var snapshot = new SimulatedTelemetryProvider().Capture();

        Assert.IsTrue(snapshot.CpuUsagePercent is >= 0 and <= 100);
        Assert.IsTrue(snapshot.GpuUsagePercent is >= 0 and <= 100);
        Assert.IsGreaterThan(0L, snapshot.TotalMemoryBytes);
        Assert.IsTrue(snapshot.UsedMemoryBytes <= snapshot.TotalMemoryBytes);
        Assert.HasCount(1, snapshot.Drives);
        Assert.AreEqual("Healthy", snapshot.HealthStatus);
    }
}
