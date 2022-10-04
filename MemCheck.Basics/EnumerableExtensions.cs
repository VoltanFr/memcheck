﻿using System;
using System.Collections.Generic;

namespace MemCheck.Basics;

public static class EnumerableExtensions
{
    public static void ForEach<T>(this IEnumerable<T> sequence, Action<T> action)
    {
        foreach (var item in sequence)
            action(item);
    }
}
