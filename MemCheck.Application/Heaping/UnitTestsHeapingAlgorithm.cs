using System;

namespace MemCheck.Application.Heaping;

internal sealed class UnitTestsHeapingAlgorithm : HeapingAlgorithm
{
    public const int ID = 3;
    public override int Id => ID;
    protected override DateTime GetExpiryUtcDate(int currentHeap, DateTime lastLearnUtcTime)
    {
        return lastLearnUtcTime.AddDays(currentHeap);
    }
}
