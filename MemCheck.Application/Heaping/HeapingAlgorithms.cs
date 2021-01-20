using MemCheck.Domain;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace MemCheck.Application.Heaping
{
    public sealed class HeapingAlgorithms
    {
        #region Fields
        private readonly ImmutableDictionary<int, HeapingAlgorithm> algorithms;
        private static readonly HeapingAlgorithms instance = new HeapingAlgorithms();
        #endregion
        #region Private methods
        private HeapingAlgorithms()
        {
            var developer = new DeveloperHeapingAlgorithm();
            var def = new DefaultHeapingAlgorithm();
            algorithms = new Dictionary<int, HeapingAlgorithm>(){
                { developer.Id, developer },
                { def.Id, def }
            }.ToImmutableDictionary();
        }
        #endregion
        public IEnumerable<int> Ids
        {
            get
            {
                return algorithms.Keys.OrderBy(key => key);
            }
        }
        public HeapingAlgorithm FromId(int heapingAlgorithmId)
        {
            if (!algorithms.ContainsKey(heapingAlgorithmId))
                throw new ArgumentException($"Unknown heaping algorithm {heapingAlgorithmId}");
            return algorithms[heapingAlgorithmId];
        }
        public static HeapingAlgorithms Instance => instance;
        public static int DefaultAlgoId => DefaultHeapingAlgorithm.ID;
    }
}
