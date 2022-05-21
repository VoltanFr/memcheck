using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace MemCheck.Application.Heaping;

public sealed class HeapingAlgorithms
{
    #region Fields
    private readonly ImmutableDictionary<int, HeapingAlgorithm> algorithms;
    #endregion
    #region Private methods
    private HeapingAlgorithms()
    {
        var developer = new DeveloperHeapingAlgorithm();
        var def = new DefaultHeapingAlgorithm();
        var unitTests = new UnitTestsHeapingAlgorithm();
        algorithms = new Dictionary<int, HeapingAlgorithm>(){
            { developer.Id, developer },
            { def.Id, def },
            { unitTests.Id, unitTests },
        }.ToImmutableDictionary();
    }
    #endregion
    public IEnumerable<int> Ids => algorithms.Keys.OrderBy(key => key);
    public HeapingAlgorithm FromId(int heapingAlgorithmId)
    {
        return !algorithms.ContainsKey(heapingAlgorithmId)
            ? throw new ArgumentException($"Unknown heaping algorithm {heapingAlgorithmId}")
            : algorithms[heapingAlgorithmId];
    }
    public static HeapingAlgorithms Instance { get; } = new();
    public static int DefaultAlgoId => DefaultHeapingAlgorithm.ID;
}
