using System;

namespace MemCheck.Application.Heaping;

internal sealed class DeveloperHeapingAlgorithm : HeapingAlgorithm
{
    public const int ID = 2;
    public override int Id => ID;
    protected override DateTime GetExpiryUtcDate(int currentHeap, DateTime lastLearnUtcTime)
    {
        return lastLearnUtcTime.AddMinutes(3);
    }
}
