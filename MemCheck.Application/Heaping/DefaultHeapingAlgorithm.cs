using System;

namespace MemCheck.Application.Heaping
{
    internal sealed class DefaultHeapingAlgorithm : HeapingAlgorithm
    {
        #region Fields
        private readonly Func<DateTime> now;
        #endregion
        #region Private methods
        protected override DateTime GetNow()
        {
            return now();
        }
        #endregion
        public DefaultHeapingAlgorithm(Func<DateTime> now)
        {
            this.now = now;
        }
        public DefaultHeapingAlgorithm() : this(() => DateTime.UtcNow)
        {
        }
        public override int Id => HeapingAlgorithms.DefaultAlgoId;
        protected override DateTime GetExpiryUtcDate(int currentHeap, DateTime lastLearnUtcTime)
        {
            var nbDaysForExpiration = Math.Pow(2, currentHeap);
            return lastLearnUtcTime.AddDays(nbDaysForExpiration);
        }
    }
}
