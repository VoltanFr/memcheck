using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MemCheck.Basics;

[TestClass()]
public class StringExtensionsTests
{
    [TestMethod()]
    public void Empty()
    {
        Assert.AreEqual("", "".Truncate(12));
    }
}
