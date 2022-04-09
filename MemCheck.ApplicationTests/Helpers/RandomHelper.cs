using MemCheck.Application.Heaping;
using MemCheck.Domain;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;

namespace MemCheck.Application.Tests.Helpers
{
    internal static class RandomHelper
    {
        #region Private methods
        private static T EntryOfEnumerable<T>(IEnumerable<T> values)
        {
            var array = values.ToImmutableArray();
            return array[RandomNumberGenerator.GetInt32(array.Length)];
        }
        #endregion
        public static int ValueNotInSet(IEnumerable<int> excludedPossibilities)
        {
            var excl = excludedPossibilities.ToImmutableHashSet();
            var result = RandomNumberGenerator.GetInt32(int.MaxValue);
            while (excl.Contains(result))
                result = RandomNumberGenerator.GetInt32(int.MaxValue);
            return result;
        }
        public static int Heap(bool notUnknown = false)
        {
            return RandomNumberGenerator.GetInt32(notUnknown ? 1 : 0, CardInDeck.MaxHeapValue);
        }
        public static int HeapingAlgorithm()
        {
            return EntryOfEnumerable(HeapingAlgorithms.Instance.Ids);
        }
        public static string String(int? length = null) //if length is null, result will be 36 chars
        {
            var result = new StringBuilder();
            do
            {
                result.Append(Guid().ToString());
            }
            while (length != null && result.Length < length);
            if (length != null)
                result.Length = length.Value;
            return result.ToString();
        }
        public static DateTime Date(DateTime? after = null)
        {
            var start = after == null ? new DateTime(1995, 1, 1) : after.Value;
            return start.AddDays(RandomNumberGenerator.GetInt32(3650)).ToUniversalTime();
        }
        public static DateTime DateBefore(DateTime d)
        {
            return d.AddDays(-RandomNumberGenerator.GetInt32(1, 3650)).ToUniversalTime();
        }
        public static int Rating(int excludedValue = 0)
        {
            while (true)
            {
                var result = RandomNumberGenerator.GetInt32(1, 5);
                if (result != excludedValue)
                    return result;
            }
        }
        public static byte[] Bytes(int length)
        {
            return RandomNumberGenerator.GetBytes(length);
        }
        public static bool Bool()
        {
            return RandomNumberGenerator.GetInt32(0, 1) == 1;
        }
        public static Guid Guid()
        {
            return System.Guid.NewGuid();
        }
    }
}
