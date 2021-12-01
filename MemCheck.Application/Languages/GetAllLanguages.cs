using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Languages
{
    public sealed class GetAllLanguages : RequestRunner<GetAllLanguages.Request, IEnumerable<GetAllLanguages.Result>>
    {
        public GetAllLanguages(CallContext callContext) : base(callContext)
        {
        }
        protected override async Task<ResultWithMetrologyProperties<IEnumerable<Result>>> DoRunAsync(Request request)
        {
            var result = await DbContext.CardLanguages.Select(language => new Result(language.Id, language.Name, DbContext.Cards.Where(card => card.CardLanguage.Id == language.Id).Count())).ToListAsync();
            return new ResultWithMetrologyProperties<IEnumerable<Result>>(result, ("ResultCount", result.Count.ToString()));
        }
        #region Request & Result
        public sealed record Request() : IRequest
        {
            public async Task CheckValidityAsync(CallContext callContext)
            {
                await Task.CompletedTask;
            }
        }
        public sealed record Result(Guid Id, string Name, int CardCount);
        #endregion
    }
}
