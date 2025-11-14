using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MemCheck.Basics;

[TestClass()]
public class ProcessServicesTests
{
    [TestMethod()]
    public void GetPeakProcessMemoryUsage()
    {
        Assert.Contains(",", ProcessServices.GetPeakProcessMemoryUsage(), System.StringComparison.InvariantCulture);
    }
}
