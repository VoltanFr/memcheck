using System.Collections.Generic;
using System.Collections.Immutable;
using MemCheck.Application.QueryValidation;

namespace MemCheck.Application.Tests.Helpers
{
    internal sealed class TestLocalizer : ILocalized
    {
        #region Fields
        private readonly IDictionary<string, string> values;
        #endregion
        public TestLocalizer(IEnumerable<KeyValuePair<string, string>>? items = null)
        {
            values = ImmutableDictionary.CreateRange(items ?? System.Array.Empty<KeyValuePair<string, string>>());
        }
        public string Get(string resourceName)
        {
            return values.ContainsKey(resourceName) ? values[resourceName] : "";
        }
    }
}