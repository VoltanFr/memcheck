using System.Diagnostics;

namespace MemCheck.Basics
{
    public static class ObjectExtensions
    {
        [DebuggerStepThrough]
        public static T[] AsArray<T>(this T t)
        {
            return new[] { t };
        }
    }
}
