using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace MemCheck.Application.Heaping.Tests
{
    [TestClass()]
    public class DefaultHeapingAlgorithmTests
    {
        #region Private field algo
        //private readonly HeapingAlgorithm algo = new DefaultHeapingAlgorithm();
        #endregion
        [TestMethod()]
        public void DefaultAlgoMustHaveFixedId()
        {
            //Do not change the id of an algo!
            Assert.AreEqual(1, new DefaultHeapingAlgorithm().Id);
        }
        [TestMethod()]
        public void HasExpiredMustCrashOnUnknownCard()
        {
            Assert.ThrowsException<ArgumentException>(() => new DefaultHeapingAlgorithm().HasExpired(0, DateTime.UtcNow, DateTime.UtcNow));
        }
        [TestMethod()]
        public void HasExpiredOnHeap1()
        {
            var lastLearnDate = DateTime.UtcNow;
            Assert.IsFalse(new DefaultHeapingAlgorithm().HasExpired(1, lastLearnDate, lastLearnDate));
            Assert.IsFalse(new DefaultHeapingAlgorithm().HasExpired(1, lastLearnDate, lastLearnDate.AddDays(1.9)));
            Assert.IsTrue(new DefaultHeapingAlgorithm().HasExpired(1, lastLearnDate, lastLearnDate.AddDays(2.1)));
        }
        [TestMethod()]
        public void HasExpiredOnRealProdCase()
        {
            var lastLearnDate = new DateTime(2020, 04, 21, 22, 43, 00).ToUniversalTime();
            Assert.IsFalse(new DefaultHeapingAlgorithm().HasExpired(3, lastLearnDate, new DateTime(2020, 04, 23, 10, 06, 00)));
        }
    }
}