using MemCheck.Application.QueryValidation;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace MemCheck.Application.Tests.Helpers
{
    internal sealed class TestLocalizer : ILocalized
    {
        #region Fields
        private readonly IDictionary<string, string> values;
        #endregion
        public TestLocalizer(params KeyValuePair<string, string>[] items)
        {
            values = ImmutableDictionary.CreateRange(items ?? System.Array.Empty<KeyValuePair<string, string>>());
        }
        [DebuggerStepThrough]
        public string Get(string resourceName)
        {
            return values.ContainsKey(resourceName) ? values[resourceName] : "";
        }
    }
}