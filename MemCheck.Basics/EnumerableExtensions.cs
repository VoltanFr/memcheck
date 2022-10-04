using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MemCheck.Basics;

public static class EnumerableExtensions
{
    public static async Task ForEachWaitingForEachAsync<TElementType>(this IEnumerable<TElementType> sequence, Func<TElementType, Task> action)
    {
        foreach (var item in sequence)
            await action(item);
    }
}
