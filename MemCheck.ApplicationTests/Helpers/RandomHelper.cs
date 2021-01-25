using MemCheck.Application.Heaping;
using MemCheck.Domain;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace MemCheck.Application.Tests.Helpers
{
    internal static class RandomHelper
    {
        #region Private field random
        private static readonly Random random = new Random();
        #endregion
        #region Private methods
        private static T Entry<T>(IEnumerable<T> values)
        {
            var array = values.ToImmutableArray();
            return array[random.Next(array.Length)];
        }
        #endregion
        public static int ValueNotInSet(IEnumerable<int> excludedPossibilities)
        {
            var excl = excludedPossibilities.ToImmutableHashSet();
            var result = random.Next();
            while (excl.Contains(result))
                result = random.Next();
            return result;
        }
        public static int Heap(bool notUnknown = false)
        {
            return random.Next(notUnknown ? 1 : 0, CardInDeck.MaxHeapValue);
        }
        public static int HeapingAlgorithm()
        {
            return Entry(HeapingAlgorithms.Instance.Ids);
        }
        public static string String(int? length = null)
        {
            var result = new StringBuilder();
            do
            {
                result.Append(Guid.NewGuid().ToString());
            }
            while (length != null && result.Length < length);
            if (length != null)
                result.Length = length.Value;
            return result.ToString();
        }
        public static DateTime DateBefore(DateTime d)
        {
            return d.AddDays(-random.Next(1, 3650));
        }
        public static Random Random => random;
    }
}
