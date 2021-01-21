using MemCheck.Basics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MemCheck.Application.CardChanging
{
    [TestClass()]
    public class StringExtensionsTests
    {
        [TestMethod()]
        public void Empty()
        {
            Assert.AreEqual("", "".Truncate(12));
        }
    }
}
