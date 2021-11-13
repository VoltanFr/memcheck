using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Languages
{
    public sealed class GetAllLanguages
    {
        #region Fields
        private readonly CallContext callContext;
        #endregion
        public GetAllLanguages(CallContext callContext)
        {
            this.callContext = callContext;
        }
        public async Task<IEnumerable<Result>> RunAsync()
        {
            var result = await callContext.DbContext.CardLanguages.Select(language => new Result(language.Id, language.Name, callContext.DbContext.Cards.Where(card => card.CardLanguage.Id == language.Id).Count())).ToListAsync();
            callContext.TelemetryClient.TrackEvent("GetAllLanguages", ("ResultCount", result.Count.ToString()));
            return result;
        }
        public sealed record Result(Guid Id, string Name, int CardCount);
    }
}
