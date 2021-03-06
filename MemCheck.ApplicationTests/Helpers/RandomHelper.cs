﻿using MemCheck.Application.Heaping;
using MemCheck.Application.Languages;
using MemCheck.Basics;
using MemCheck.Domain;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace MemCheck.Application.Tests.Helpers
{
    internal static class RandomHelper
    {
        #region Private methods
        private static T Entry<T>(IEnumerable<T> values)
        {
            var array = values.ToImmutableArray();
            return array[Randomizer.Next(array.Length)];
        }
        #endregion
        public static int ValueNotInSet(IEnumerable<int> excludedPossibilities)
        {
            var excl = excludedPossibilities.ToImmutableHashSet();
            var result = Randomizer.Next();
            while (excl.Contains(result))
                result = Randomizer.Next();
            return result;
        }
        public static int Heap(bool notUnknown = false)
        {
            return Randomizer.Next(notUnknown ? 1 : 0, CardInDeck.MaxHeapValue);
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
        public static string CultureName()
        {
            return String(SetUserUILanguage.Request.MinNameLength);
        }
        public static DateTime Date(DateTime? after = null)
        {
            var start = after == null ? new DateTime(1995, 1, 1) : after.Value;
            return start.AddDays(Randomizer.Next(3650)).ToUniversalTime();
        }
        public static DateTime DateBefore(DateTime d)
        {
            return d.AddDays(-Randomizer.Next(1, 3650)).ToUniversalTime();
        }
        public static int Rating()
        {
            return Randomizer.Next(1, 5);
        }
        public static byte[] Bytes(int length)
        {
            var b = new byte[length];
            Randomizer.NextBytes(b);
            return b;
        }
    }
}
