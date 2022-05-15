using MemCheck.Application.QueryValidation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards
{
    public sealed class GetRecentDemoUses : RequestRunner<GetRecentDemoUses.Request, GetRecentDemoUses.Result>
    {
        #region Fields
        private readonly DateTime now;
        #endregion
        public GetRecentDemoUses(CallContext callContext, DateTime? now = null) : base(callContext)
        {
            this.now = now ?? DateTime.UtcNow;
        }
        protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
        {
            var oldestIncludedDate = now - TimeSpan.FromDays(request.DayCount);
            var resultEntries = await DbContext.DemoDownloadAuditTrailEntries.AsNoTracking().Where(entry => entry.DownloadUtcDate >= oldestIncludedDate).Select(entry => new ResultEntry(entry.TagId, entry.DownloadUtcDate, entry.CountOfCardsReturned)).ToListAsync();
            var result = new Result(resultEntries.ToImmutableArray());

            return new ResultWithMetrologyProperties<Result>(result,
               IntMetric("DayCount", request.DayCount),
               IntMetric("ResultCount", resultEntries.Count));
        }
        #region Request and Result
        public sealed record Request(int DayCount) : IRequest
        {
            public async Task CheckValidityAsync(CallContext callContext)
            {
                if (DayCount is < 1 or > 31) // A big count is not a problem, but probably an error
                    throw new RequestInputException($"Invalid DayCount: {DayCount}");
                await Task.CompletedTask;
            }
        }
        public sealed record Result(ImmutableArray<ResultEntry> Entries);
        public sealed record ResultEntry(Guid TagId, DateTime DownloadUtcDate, int CountOfCardsReturned);
        #endregion
    }
}
