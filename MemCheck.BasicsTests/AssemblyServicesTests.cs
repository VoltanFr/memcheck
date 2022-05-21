using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MemCheck.Basics;

[TestClass()]
public class AssemblyServicesTests
{
    [TestMethod()]
    public void CheckNull()
    {
        Assert.AreEqual("Unknown", AssemblyServices.GetDisplayInfoForAssembly(null));
    }
    [TestMethod()]
    public void CheckBasics()
    {
        Assert.AreEqual("MemCheck.BasicsTests 0.12.12", AssemblyServices.GetDisplayInfoForAssembly(GetType().Assembly));
    }
}
