using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vanta.ViewModels;

namespace Vanta.Tests;

[TestClass]
public sealed class DashboardFormattingTests
{
    [TestMethod]
    [DataRow(0L, "0 GB")]
    [DataRow(1_073_741_824L, "1 GB")]
    [DataRow(1_649_267_441_664L, "1.5 TB")]
    public void FormatBytes_UsesReadableBinaryUnits(long value, string expected)
    {
        Assert.AreEqual(expected, DashboardViewModel.FormatBytes(value));
    }
}
