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
        Assert.IsFalse(list.Any());
    }
    [TestMethod()]
    public async Task Times_Negative()
    {
        await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(async () => await (-1).TimesAsync(async () => { await Task.CompletedTask; }));
    }
    [TestMethod()]
    public async Task Times_Once()
    {
        var list = new List<int>();
        await 1.TimesAsync(async () => { list.Add(0); await Task.CompletedTask; });
        Assert.AreEqual(1, list.Count);
    }
    [TestMethod()]
    public async Task Times_Random()
    {
        var list = new List<int>();
        var count = RandomNumberGenerator.GetInt32(3, 10);
        await count.TimesAsync(async () => { await Task.Delay(1); list.Add(0); });
        Assert.AreEqual(count, list.Count);
    }
}
