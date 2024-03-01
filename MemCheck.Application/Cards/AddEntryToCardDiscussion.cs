using MemCheck.Application.QueryValidation;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards;

public sealed class AddEntryToCardDiscussion : RequestRunner<AddEntryToCardDiscussion.Request, AddEntryToCardDiscussion.Result>
{
    #region Fields
    private readonly DateTime runUtcDate;
    #endregion
    public AddEntryToCardDiscussion(CallContext callContext, DateTime? runUtcDate = null) : base(callContext)
    {
        this.runUtcDate = runUtcDate ?? DateTime.UtcNow;
    }
    protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
    {
        var user = await DbContext.Users.Where(user => user.Id == request.UserId).SingleAsync();
        var card = await DbContext.Cards.Where(card => card.Id == request.CardId).Include(card => card.LatestDiscussionEntry).FirstAsync();

        var entry = new CardDiscussionEntry()
        {
            Card = request.CardId,
            Creator = user,
            Text = request.Text,
            CreationUtcDate = runUtcDate,
            PreviousVersion = null,
            PreviousEntry = card.LatestDiscussionEntry
        };

        await DbContext.CardDiscussionEntries.AddAsync(entry);

        card.LatestDiscussionEntry = entry;

        await DbContext.SaveChangesAsync();

        var entryCountForCard = await DbContext.CardDiscussionEntries.AsNoTracking().Where(entry => entry.Card == request.CardId).CountAsync();

        return new ResultWithMetrologyProperties<Result>(new Result(entry.Id, entryCountForCard), ("DiscussionEntryId", entry.Id.ToString()), ("CardId", card.Id.ToString()));
    }
    #region Request & Result records
    public sealed record Request(Guid UserId, Guid CardId, string Text) : IRequest
    {
        public async Task CheckValidityAsync(CallContext callContext)
        {
            await QueryValidationHelper.CheckUserExistsAsync(callContext.DbContext, UserId);
            await QueryValidationHelper.CheckCardExistsAsync(callContext.DbContext, CardId);
            CardVisibilityHelper.CheckUserIsAllowedToViewCard(callContext.DbContext, UserId, CardId);
            QueryValidationHelper.CheckCanCreateCardDiscussionEntryWithText(Text);
        }
    }
    public sealed record Result(Guid EntryId, int EntryCountForCard);
    #endregion
}
