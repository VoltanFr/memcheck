using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace MemCheck.Basics;

public static class IntExtensions
{
    public static void Times(this int count, Action action)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), $"Can't do {count} times");
        for (var i = 0; i < count; i++)
            action();
    }
    public static ImmutableArray<TResult> Times<TResult>(this int count, Func<TResult> func)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), $"Can't do {count} times");
        var result = new List<TResult>();
        for (var i = 0; i < count; i++)
            result.Add(func());
        return result.ToImmutableArray();
    }
    public static async Task TimesAsync(this int count, Func<Task> action)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), $"Can't do {count} times");
        for (var i = 0; i < count; i++)
            await action();
    }
    public static async Task<ImmutableArray<TResult>> TimesAsync<TResult>(this int count, Func<Task<TResult>> func)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), $"Can't do {count} times");
        var result = new List<TResult>();
        for (var i = 0; i < count; i++)
            result.Add(await func());
        return result.ToImmutableArray();
    }
}
