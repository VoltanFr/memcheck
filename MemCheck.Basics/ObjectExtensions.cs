namespace MemCheck.Basics
{
    public static class ObjectExtensions
    {
        public static T[] AsArray<T>(this T t)
        {
            return new[] { t };
        }
    }
}
