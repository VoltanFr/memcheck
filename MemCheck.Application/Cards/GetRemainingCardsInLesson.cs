using MemCheck.Application.QueryValidation;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards;

public sealed class GetRemainingCardsInLesson : RequestRunner<GetRemainingCardsInLesson.Request, GetRemainingCardsInLesson.Result>
{
    #region Fields
    private readonly DateTime runDate;
    #endregion
    public GetRemainingCardsInLesson(CallContext callContext, DateTime? runDate = null) : base(callContext)
    {
        this.runDate = runDate == null ? DateTime.UtcNow : runDate.Value;
    }
    protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
    {
        var result = await DbContext.CardsInDecks.AsNoTracking()
            .Where(cardInDeck =>
                cardInDeck.DeckId == request.DeckId
                && (request.LessonModeIsUnknown ? cardInDeck.CurrentHeap == CardInDeck.UnknownHeap : cardInDeck.CurrentHeap != CardInDeck.UnknownHeap)
                && (request.LessonModeIsUnknown || cardInDeck.ExpiryUtcTime <= runDate))
            .CountAsync();

        return new ResultWithMetrologyProperties<Result>(new Result(result), ("DeckId", request.DeckId.ToString()));
    }
    #region Request and Result
    public sealed record Request(Guid CurrentUserId, Guid DeckId, bool LessonModeIsUnknown) : IRequest
    {
        public async Task CheckValidityAsync(CallContext callContext)
        {
            QueryValidationHelper.CheckNotReservedGuid(CurrentUserId);
            QueryValidationHelper.CheckNotReservedGuid(DeckId);
            await QueryValidationHelper.CheckUserIsOwnerOfDeckAsync(callContext.DbContext, CurrentUserId, DeckId);
        }
    }
    public sealed record Result(int Count);
    #endregion
}
