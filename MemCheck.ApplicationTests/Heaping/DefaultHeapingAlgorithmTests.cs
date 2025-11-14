using MemCheck.Application.Helpers;
using MemCheck.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MemCheck.Application.Heaping;

[TestClass()]
public class DefaultHeapingAlgorithmTests
{
    #region Private stuff
    private readonly HeapingAlgorithm algo = new DefaultHeapingAlgorithm();
    private static IEnumerable<object[]> AllPossibleHeaps()
    {
        return Enumerable.Range(1, CardInDeck.MaxHeapValue).Select(i => new object[] { i });
    }
    #endregion
    [TestMethod()]
    public void DefaultAlgoMustHaveFixedId()
    {
        //Do not change the id of an algo! See comment in DefaultHeapingAlgorithm
        Assert.AreEqual(1, algo.Id);
    }
    [TestMethod()]
    public void UnknownCard()
    {
        Assert.ThrowsExactly<ArgumentException>(() => algo.ExpiryUtcDate(0, RandomHelper.Date()));
    }
    [TestMethod()]
    public void NonUtc()
    {
        Assert.ThrowsExactly<ArgumentException>(() => algo.ExpiryUtcDate(0, DateTime.Now));
    }
    [TestMethod, DynamicData(nameof(AllPossibleHeaps), DynamicDataSourceType.Method)]
    public void ExpiryDateInCorrectInterval(int heap)
    {
        var lastLearnDate = RandomHelper.Date();
        var expiryDate = algo.ExpiryUtcDate(heap, lastLearnDate);
        var nbDaysForExpiration = Math.Pow(2, heap);
        DateAssert.IsInRange(lastLearnDate.AddDays(nbDaysForExpiration), TimeSpan.FromMinutes(nbDaysForExpiration * 10), expiryDate);
    }
    [TestMethod, DynamicData(nameof(AllPossibleHeaps), DynamicDataSourceType.Method)]
    public void ExpiryDateIsRandom(int heap)
    {
        var lastLearnDate = RandomHelper.Date();
        var expiryDate = algo.ExpiryUtcDate(heap, lastLearnDate);
        for (var i = 0; i < 10; i++)
            if (algo.ExpiryUtcDate(heap, lastLearnDate) != expiryDate)
                return;
        Assert.Fail("Always got the same expiry date");
    }
}
