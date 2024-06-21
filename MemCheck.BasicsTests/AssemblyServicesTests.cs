using System;
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
        StringAssert.StartsWith(AssemblyServices.GetDisplayInfoForAssembly(GetType().Assembly), "MemCheck.BasicsTests 0.12.13", StringComparison.Ordinal);
    }
}
