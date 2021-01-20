using MemCheck.Domain;
using System;

namespace MemCheck.Application.Heaping
{
    internal sealed class DefaultHeapingAlgorithm : HeapingAlgorithm
    {
        public const int ID = Deck.DefaultHeapingAlgorithmId;
        public override int Id => ID;
        protected override DateTime GetExpiryUtcDate(int currentHeap, DateTime lastLearnUtcTime)
        {
            var nbDaysForExpiration = Math.Pow(2, currentHeap);
            return lastLearnUtcTime.AddDays(nbDaysForExpiration);
        }
    }
}
