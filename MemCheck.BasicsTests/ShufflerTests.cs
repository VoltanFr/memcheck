using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace MemCheck.Basics.Tests
{
    [TestClass()]
    public class ShufflerTests
    {
        #region Private method
        private static ImmutableArray<Guid> GetGuids()
        {
            return Enumerable.Range(1, 10000).Select(i => Guid.NewGuid()).ToImmutableArray();
        }
        #endregion
        [TestMethod()]
        public void Empty()
        {
            Assert.AreEqual(0, Shuffler.Shuffle(Array.Empty<Guid>()).Count());
        }
        [TestMethod()]
        public void OneEntry()
        {
            var elem = Guid.NewGuid();
            Assert.AreEqual(elem, Shuffler.Shuffle(elem.ToEnumerable()).Single());
        }
        [TestMethod()]
        public void NoElementLost()
        {
            var input = GetGuids();
            var shuffled = Shuffler.Shuffle(input);
            Assert.IsTrue(input.ToHashSet().SetEquals(shuffled.ToHashSet()));
        }
        [TestMethod()]
        public void NotTheSameOrderOnSuccessiveRuns()
        {
            var input = GetGuids();
            var firstRun = Shuffler.Shuffle(input);
            Assert.AreEqual(input.Length, firstRun.Count());
            var secondRun = Shuffler.Shuffle(input);
            var thirdRun = Shuffler.Shuffle(input);
            Assert.IsFalse(firstRun.SequenceEqual(secondRun));
            Assert.IsFalse(firstRun.SequenceEqual(thirdRun));
            Assert.IsFalse(secondRun.SequenceEqual(thirdRun));
        }
    }
}
