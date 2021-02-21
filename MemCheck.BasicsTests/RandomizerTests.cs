using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MemCheck.Basics.Tests
{
    // The multi thread test fails if you remove the locks in Randomizer
    [TestClass()]
    public class RandomizerTests
    {
        [TestMethod()]
        public void CheckRandomnessMonoThread()
        {
            var countPerValue = Enumerable.Range(1, 100000).Select(i => Randomizer.Next()).GroupBy(v => v);
            Assert.IsFalse(countPerValue.Any(cpv => cpv.Count() > 2));
        }
        [TestMethod()]
        public void CheckRandomnessMultiThread()
        {
            var values = new Dictionary<int, int>();
            const int testCount = 100000;
            var executed = 0;

            for (int i = 0; i < testCount; i++)
                ThreadPool.QueueUserWorkItem(o =>
                {
                    var r = Randomizer.Next(1, int.MaxValue);
                    lock (values)
                    {
                        if (values.ContainsKey(r))
                            values[r] = values[r] + 1;
                        else
                            values.Add(r, 0);
                    }
                    Interlocked.Increment(ref executed);
                });

            while (executed < testCount)
                Thread.Sleep(5);

            Assert.IsFalse(values.Values.Any(count => count > 2));
        }
    }
}