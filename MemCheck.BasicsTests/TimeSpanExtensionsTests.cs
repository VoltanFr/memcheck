using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MemCheck.Basics;

[TestClass()]
public class TimeSpanExtensionsTests
{
    [TestMethod()]
    public void ToStringWithoutMs()
    {
        Assert.AreEqual("00:00:01", TimeSpan.FromMilliseconds(1059).ToStringWithoutMs());
        Assert.AreEqual("18:59:59", new TimeSpan(2, 18, 59, 59, 999).ToStringWithoutMs());
    }
}
