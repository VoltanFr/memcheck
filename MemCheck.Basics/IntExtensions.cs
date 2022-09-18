using System;
using System.Threading.Tasks;

namespace MemCheck.Basics;

public static class IntExtensions
{
    public static async Task TimesAsync(this int count, Func<Task> action)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), $"Can't do {count} times");
        for (var i = 0; i < count; i++)
            await action();
    }
}
