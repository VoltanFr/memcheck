using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace MemCheck.Application.Tests.Helpers
{
    internal static class RandomHelper
    {
        #region Private field random
        private static readonly Random random = new Random();
        #endregion
        public static int ValueNotInSet(IEnumerable<int> excludedPossibilities)
        {
            var excl = excludedPossibilities.ToImmutableHashSet();
            var result = random.Next();
            while (excl.Contains(result))
                result = random.Next();
            return result;
        }
        public static T Entry<T>(IEnumerable<T> values)
        {
            var array = values.ToImmutableArray();
            return array[random.Next(array.Length)];
        }
    }
}
