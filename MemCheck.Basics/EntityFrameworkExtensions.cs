using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using System;

namespace MemCheck.Basics;

public static class EntityFrameworkExtensions
{
    public static async Task<ImmutableArray<TSource>> ToImmutableArrayAsync<TSource>([NotNull] this IQueryable<TSource> source, CancellationToken cancellationToken = default)
    {
        return (await source.ToListAsync(cancellationToken).ConfigureAwait(false)).ToImmutableArray();
    }
    public static async Task<ImmutableDictionary<TKey, TElement>> ToImmutableDictionaryAsync<TSource, TKey, TElement>(this IQueryable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, CancellationToken cancellationToken = default) where TKey : notnull
    {
        return (await source.ToDictionaryAsync(keySelector, elementSelector, cancellationToken)).ToImmutableDictionary();
    }
}
