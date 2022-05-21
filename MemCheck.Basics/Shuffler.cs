using System;
using System.Collections.Generic;
using System.Linq;

namespace MemCheck.Basics;

public static class Shuffler
{
    public static IEnumerable<T> Shuffle<T>(IEnumerable<T> source)
    {
        return source.OrderBy(elem => Guid.NewGuid());
    }
}
