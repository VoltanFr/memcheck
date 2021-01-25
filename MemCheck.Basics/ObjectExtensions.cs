using System.Collections.Generic;

namespace MemCheck.Basics
{
    public static class ObjectExtensions
    {
        public static T[] ToEnumerable<T>(this T t)
        {
            return new[] { t };
        }
    }
}
