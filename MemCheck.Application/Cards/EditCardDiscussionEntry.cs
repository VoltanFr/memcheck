using MemCheck.Application.QueryValidation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards;

public sealed class EditCardDiscussionEntry : RequestRunner<EditCardDiscussionEntry.Request, EditCardDiscussionEntry.Result>
{
    #region Fields
    private readonly DateTime runUtcDate;
    #endregion
    public EditCardDiscussionEntry(CallContext callContext, DateTime? runUtcDate = null) : base(callContext)
    {
        this.runUtcDate = runUtcDate ?? DateTime.UtcNow;
    }
    protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
    {
        var previousVersionCreator = new PreviousCardDiscussionEntryVersionCreator(DbContext);
        var entry = await previousVersionCreator.RunAsync(request.EntryId, runUtcDate);
        entry.Text = request.Text;
        await DbContext.SaveChangesAsync();
        return new ResultWithMetrologyProperties<Result>(new Result(), ("EntryId", request.EntryId.ToString()));
    }
    #region Request & Result records
    public sealed record Request(Guid UserId, Guid EntryId, string Text) : IRequest
    {
        public async Task CheckValidityAsync(CallContext callContext)
        {
            await QueryValidationHelper.CheckUserExistsAsync(callContext.DbContext, UserId);

            var entry = await callContext.DbContext.CardDiscussionEntries.AsNoTracking()
                .Include(entry => entry.Creator)
                .SingleOrDefaultAsync(entry => entry.Id == EntryId);
            if (entry == null)
                throw new NonexistentCardDiscussionEntryException();

            await QueryValidationHelper.CheckCardExistsAsync(callContext.DbContext, entry.Card); // Not really needed, and not checked by a unit test - just a security
            CardVisibilityHelper.CheckUserIsAllowedToViewCard(callContext.DbContext, UserId, entry.Card);
            QueryValidationHelper.CheckCanCreateCardDiscussionEntryWithText(Text);
            QueryValidationHelper.CheckUserCanEditCardDiscussionEntry(UserId, entry);
        }
    }
    public sealed record Result();
    #endregion
}
