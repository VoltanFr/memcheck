using System.Collections.Generic;
using System.Collections.Immutable;
using MemCheck.Application.QueryValidation;
using Microsoft.Extensions.Localization;

namespace MemCheck.Application.Tests.BasicHelpers
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
        public LocalizedString this[string name] => values.ContainsKey(name) ? new LocalizedString(name, values[name]) : new LocalizedString(name, "");
        public LocalizedString this[string name, params object[] arguments] => new LocalizedString(name, "");

        public IStringLocalizer Localizer => throw new System.NotImplementedException();

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            return new LocalizedString[0];
        }

        public string Get(string resourceName)
        {
            return this[resourceName].Value;
        }
    }
}