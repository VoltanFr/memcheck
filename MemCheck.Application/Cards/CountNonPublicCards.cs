using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards;

public sealed class CountNonPublicCards : RequestRunner<CountNonPublicCards.Request, CountNonPublicCards.Result>
{
    public CountNonPublicCards(CallContext callContext) : base(callContext)
    {
    }
    protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
    {
        var count = await DbContext.Cards
            .AsNoTracking()
            .Where(card => card.UsersWithView.Any())
            .CountAsync();

        return new ResultWithMetrologyProperties<Result>(new Result(count), IntMetric("ResultCount", count));
    }
    #region Request and result classes
    public sealed record Request : IRequest
    {
        public async Task CheckValidityAsync(CallContext callContext)
        {
            await Task.CompletedTask;
        }
    }
    public sealed record Result(int Count);
    #endregion
}
