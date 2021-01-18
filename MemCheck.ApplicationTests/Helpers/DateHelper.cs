using System;

namespace MemCheck.Application.Tests.Helpers
{
    public static class DateHelper
    {
        #region Private field random
        private static readonly Random random = new Random();
        #endregion
        public static DateTime Random(DateTime? after = null)
        {
            var start = after == null ? new DateTime(1995, 1, 1) : after.Value;
            return start.AddDays(random.Next(3650));
        }
    }
}
