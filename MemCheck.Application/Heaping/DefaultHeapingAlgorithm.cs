using System;

namespace MemCheck.Application.Heaping
{
    internal sealed class DefaultHeapingAlgorithm : HeapingAlgorithm
    {
        public override int Id => HeapingAlgorithms.DefaultAlgoId;
        protected override DateTime GetExpiryUtcDate(int currentHeap, DateTime lastLearnUtcTime)
        {
            var nbDaysForExpiration = Math.Pow(2, currentHeap);
            return lastLearnUtcTime.AddDays(nbDaysForExpiration);
        }
    }
}
