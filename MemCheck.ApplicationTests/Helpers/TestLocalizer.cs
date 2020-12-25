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
            values = ImmutableDictionary.CreateRange(items ?? new KeyValuePair<string, string>[0]);
        }
        public string Get(string resourceName)
        {
            return values.ContainsKey(resourceName) ? values[resourceName] : "";
        }
    }
}