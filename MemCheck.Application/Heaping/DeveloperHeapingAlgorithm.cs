using System;

namespace MemCheck.Application.Heaping
{
    internal sealed class DeveloperHeapingAlgorithm : HeapingAlgorithm
    {
        public override int Id => 2;
        protected override DateTime GetExpiryUtcDate(int currentHeap, DateTime lastLearnUtcTime)
        {
            return lastLearnUtcTime.AddSeconds(30);
        }
    }
}
