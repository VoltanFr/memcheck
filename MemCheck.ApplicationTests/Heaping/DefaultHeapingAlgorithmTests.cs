using MemCheck.Application.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace MemCheck.Application.Heaping
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
        public void UnknownCard()
        {
            Assert.ThrowsException<ArgumentException>(() => new DefaultHeapingAlgorithm().ExpiryUtcDate(0, RandomHelper.Date()));
        }
        [TestMethod()]
        public void NonUtc()
        {
            Assert.ThrowsException<ArgumentException>(() => new DefaultHeapingAlgorithm().ExpiryUtcDate(0, DateTime.Now));
        }
        [TestMethod()]
        public void ExpiryUtcDate()
        {
            var d = RandomHelper.Date();
            Assert.AreEqual(d.AddDays(2), new DefaultHeapingAlgorithm().ExpiryUtcDate(1, d));
            Assert.AreEqual(d.AddDays(4), new DefaultHeapingAlgorithm().ExpiryUtcDate(2, d));
            Assert.AreEqual(d.AddDays(1024), new DefaultHeapingAlgorithm().ExpiryUtcDate(10, d));
        }
    }
}
