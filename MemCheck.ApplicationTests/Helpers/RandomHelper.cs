﻿using MemCheck.Application.Heaping;
using MemCheck.Domain;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;

namespace MemCheck.Application.Helpers;

public static class RandomHelper
{
    #region Private methods
    private static T EntryOfEnumerable<T>(IEnumerable<T> values)
    {
        var array = values.ToImmutableArray();
        return array[Int(array.Length)];
    }
    #endregion
    public static int ValueNotInSet(IEnumerable<int> excludedPossibilities)
    {
        var excl = excludedPossibilities.ToImmutableHashSet();
        var result = Int(int.MaxValue);
        while (excl.Contains(result))
            result = Int(int.MaxValue);
        return result;
    }
    public static int Heap(bool notUnknown = false)
    {
        return Int(notUnknown ? 1 : 0, CardInDeck.MaxHeapValue);
    }
    public static int HeapingAlgorithm()
    {
        return EntryOfEnumerable(HeapingAlgorithms.Instance.Ids);
    }
    public static string String(int? length = null, bool firstCharMustBeLetter = false) //if length is null, result will be 36 chars
    {
        var result = new StringBuilder();
        if (firstCharMustBeLetter)
            result.Append(Letter());
        do
        {
            result.Append(Guid().ToString());
        }
        while (length != null && result.Length < length);
        if (length != null)
            result.Length = length.Value;

        // We know that here result contains only lower chars (as per Guid().ToString()): let's change that randomly
        for (var charIndex = 0; charIndex < result.Length; charIndex++)
            if (Bool())
                result[charIndex] = char.ToUpperInvariant(result[charIndex]);

        return result.ToString();
    }
    public static DateTime Date(DateTime? after = null)
    {
        var start = after == null ? new DateTime(1995, 1, 1) : after.Value;
        return start.AddDays(Int(3650)).ToUniversalTime();
    }
    public static DateTime DateBefore(DateTime d)
    {
        return d.AddDays(-Int(1, 3650)).ToUniversalTime();
    }
    public static int Rating(int excludedValue = 0)
    {
        while (true)
        {
            var result = Int(1, 5);//To be updated
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
        return Int(0, 2) == 1;
    }
    public static Guid Guid()
    {
        return System.Guid.NewGuid();
    }
    public static int Int(int toExclusive)
    {
        return Int(0, toExclusive);
    }
    public static int Int(int fromInclusive, int toExclusive)
    {
        return RandomNumberGenerator.GetInt32(fromInclusive, toExclusive);
    }
    public static char Letter(bool lowerCase = false)
    {
        while (true)
        {
            var result = (char)Int(255);
            if (result is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z'))
                return lowerCase ? char.ToLowerInvariant(result) : result;
        }
    }
    public static string Password()
    {
        return String().ToUpperInvariant() + Letter(true) + String();
    }
    public static string Email()
    {
        return String(5) + '@' + String(5);
    }
    public static string UserName()
    {
        return String(firstCharMustBeLetter: true);
    }
}
