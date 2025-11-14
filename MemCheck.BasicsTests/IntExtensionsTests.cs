using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MemCheck.Basics;

[TestClass()]
public class IntExtensionsTests
{
    [TestMethod()]
    public async Task Times_Zero()
    {
        var list = new List<int>();
        await 0.TimesAsync(async () => { list.Add(0); await Task.CompletedTask; });
        Assert.IsEmpty(list);
    }
    [TestMethod()]
    public async Task Times_Negative()
    {
        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(async () => await (-1).TimesAsync(async () => { await Task.CompletedTask; }));
    }
    [TestMethod()]
    public async Task Times_Once()
    {
        var list = new List<int>();
        await 1.TimesAsync(async () => { list.Add(0); await Task.CompletedTask; });
        Assert.HasCount(1, list);
    }
    [TestMethod()]
    public async Task Times_Random()
    {
        var list = new List<int>();
        var count = RandomNumberGenerator.GetInt32(3, 10);
        await count.TimesAsync(async () => { await Task.Delay(1); list.Add(0); });
        Assert.AreEqual(count, list.Count);
    }
    [TestMethod()]
    public async Task TimesWithResult_Zero()
    {
        var result = await 0.TimesAsync(async () => { await Task.CompletedTask; return 12; });
        Assert.IsFalse(result.Any());
    }
    [TestMethod()]
    public async Task TimessWithResult_Negative()
    {
        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(async () => await (-1).TimesAsync(async () => { await Task.CompletedTask; return 1; }));
    }
    [TestMethod()]
    public async Task TimesWithResult_Once()
    {
        var result = await 1.TimesAsync(async () => { await Task.CompletedTask; return 1; });
        Assert.HasCount(1, result);
        Assert.AreEqual(1, result[0]);
    }
    [TestMethod()]
    public async Task TimesWithResult_Random()
    {
        var expectedCount = RandomNumberGenerator.GetInt32(3, 10);
        var result = await expectedCount.TimesAsync(async () => { await Task.CompletedTask; return expectedCount; });
        Assert.AreEqual(expectedCount, result.Length);
        foreach (var item in result)
            Assert.AreEqual(expectedCount, item);
    }
}
