using System;
using System.Collections.Generic;
using System.Linq;

namespace MemCheck.Application.QueryValidation;

internal static class ComparisonHelper
{
    public static bool SameSetOfGuid(IEnumerable<Guid> left, IEnumerable<Guid> right)
    {
        return left.ToHashSet().SetEquals(right.ToHashSet());
    }
}
