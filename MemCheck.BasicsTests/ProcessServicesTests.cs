using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MemCheck.Basics;

[TestClass()]
public class ProcessServicesTests
{
    [TestMethod()]
    public void GetPeakProcessMemoryUsage()
    {
        StringAssert.Contains(ProcessServices.GetPeakProcessMemoryUsage(), ",", System.StringComparison.InvariantCulture);
    }
}
