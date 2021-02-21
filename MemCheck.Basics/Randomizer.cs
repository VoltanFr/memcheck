using System;

namespace MemCheck.Basics
{
    /* Thread-safe random generator.
     * Ok to use as long as there is no concern about lock performance, which is the case in MemCheck.
     */
    public static class Randomizer
    {
        #region Private field
        private static readonly Random random = new Random();
        #endregion
        public static int Next()
        {
            lock (random)
                return random.Next();
        }
        public static int Next(int maxValue)
        {
            lock (random)
                return random.Next(maxValue);
        }
        public static int Next(int minValue, int maxValue)
        {
            lock (random)
                return random.Next(minValue, maxValue);
        }
        public static void NextBytes(byte[] buffer)
        {
            lock (random)
                random.NextBytes(buffer);
        }
    }
}
