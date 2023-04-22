using MemCheck.Basics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace MemCheck.Application.Helpers;

[TestClass()]
public class RandomHelperTests
{
    [TestMethod()]
    public void Ints()
    {
        var attempts = RandomHelper.Int(100, 1000);
        var ints = attempts.Times(() => RandomHelper.Int(-1000, 1000));
        var first = ints.First();
        Assert.IsFalse(ints.All(i => i == first), $"Imong {attempts} attempts, Int returned {first}");
    }
    [TestMethod()]
    public void Bool()
    {
        var attempts = RandomHelper.Int(100, 1000);
        var bools = attempts.Times(() => RandomHelper.Bool());
        Assert.IsFalse(bools.All(b => b), $"Among {attempts} attempts, Bool returned only true");
        Assert.IsFalse(bools.All(b => !b), $"Among {attempts} attempts, Bool returned only false");
    }
    [TestMethod()]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "<Pending>")]
    public void LongStringContainsUpperAndLower()
    {
        var length = RandomHelper.Int(100, 1000);
        var s = RandomHelper.String(length);
        Assert.AreEqual(length, s.Length);
        Assert.IsFalse(s.ToUpperInvariant() == s, $"A string of {length} chars was all upper chars");
        Assert.IsFalse(s.ToLowerInvariant() == s, $"A string of {length} chars was all lower chars");
    }
    [TestMethod()]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "<Pending>")]
    public void ShortStringContainsUpperAndLower()
    {
        var attempts = RandomHelper.Int(100, 1000);
        var strings = attempts.Times(() => RandomHelper.String(RandomHelper.Int(2, 5)));
        Assert.IsFalse(strings.All(s => s.ToUpperInvariant() == s), $"Among {attempts} attempts, short strings were only upper");
        Assert.IsFalse(strings.All(s => s.ToLowerInvariant() == s), $"Among {attempts} attempts, short strings were only lower");
    }
    [TestMethod()]
    public void Letter_AnyCase()
    {
        for (var i = 0; i < 100; i++)
        {
            var letter = RandomHelper.Letter();
            Assert.IsTrue(char.IsLetter(letter), $"Not letter: '{letter}'");
        }
    }
    [TestMethod()]
    public void Letter_Lower()
    {
        for (var i = 0; i < 100; i++)
        {
            var letter = RandomHelper.Letter(true);
            Assert.IsTrue(char.IsLetter(letter), $"Not letter: '{letter}'");
            Assert.IsTrue(char.IsLower(letter), $"Not lower: '{letter}'");
        }
    }
}
