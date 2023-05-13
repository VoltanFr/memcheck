using MemCheck.Application.QueryValidation;
using MemCheck.Basics;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards;

public sealed class GetCardDiscussionEntries : RequestRunner<GetCardDiscussionEntries.Request, GetCardDiscussionEntries.Result>
{
    #region Fields
    #endregion
    private static ImmutableArray<Guid> OrderEntries(Guid? cardLatestDiscussionEntry, ImmutableDictionary<Guid, Guid?> allEntriesForCard)
    {
        var currentEntryId = cardLatestDiscussionEntry;
        var allEntriesIdsOrderedList = new List<Guid>();
        while (currentEntryId != null)
        {
            allEntriesIdsOrderedList.Add(currentEntryId.Value);
            currentEntryId = allEntriesForCard[currentEntryId.Value];
        }
        return allEntriesIdsOrderedList.ToImmutableArray();
    }
    public GetCardDiscussionEntries(CallContext callContext) : base(callContext)
    {
    }
    protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
    {
        var card = await DbContext.Cards.Where(card => card.Id == request.CardId).Include(card => card.LatestDiscussionEntry).FirstAsync();

        var allEntriesForCard = await DbContext.CardDiscussionEntries.Include(entry => entry.PreviousEntry).AsNoTracking()
            .Where(entry => entry.Card == request.CardId)
            .ToImmutableDictionaryAsync(entry => entry.Id, entry => entry.PreviousEntry?.Id);

        var pageCount = (int)Math.Ceiling((double)allEntriesForCard.Count / request.PageSize);

        var allEntriesIdsOrdered = OrderEntries(card.LatestDiscussionEntry?.Id, allEntriesForCard);

        var skipCount = allEntriesIdsOrdered.IndexOf(request.LastObtainedEntry);
        if (skipCount == -1) // This happens for the first load of a discussion (or for a bad call)
            skipCount = 0;
        else
            skipCount++; // Because IndexOf is zero-based

        var resultEntryIds = allEntriesIdsOrdered
            .Skip(skipCount)
            .Take(request.PageSize)
            .ToImmutableHashSet();

        var resultEntries = await DbContext.CardDiscussionEntries.AsNoTracking()
            .Where(entry => entry.Card == request.CardId && resultEntryIds.Contains(entry.Id)) // The check on CardId is just for perf
            .Select(entry => new ResultEntry(entry.Id, entry.Creator, entry.Text, entry.CreationUtcDate, false)) // false: to be implemented
            .ToImmutableArrayAsync();

        var resultEntriesOrdered = resultEntries.OrderBy(entry => allEntriesIdsOrdered.IndexOf(entry.Id)).ToImmutableArray();

        var result = new Result(allEntriesForCard.Count, pageCount, resultEntriesOrdered);

        return new ResultWithMetrologyProperties<Result>(result,
            IntMetric("PageSize", request.PageSize),
            GuidMetric("UserId", request.UserId),
            GuidMetric("CardId", request.CardId),
            IntMetric("ResultTotalCount", result.TotalCount),
            IntMetric("ResultPageCount", result.PageCount),
            IntMetric("ResultEntryCount", result.Entries.Length));
    }
    #region Request & Result types
    public sealed record Request(Guid UserId, Guid CardId, int PageSize, Guid LastObtainedEntry) : IRequest
    {
        public const int MinPageSize = 1;
        public const int MaxPageSize = 100;
        public async Task CheckValidityAsync(CallContext callContext)
        {
            if (PageSize < MinPageSize)
                throw new PageSizeTooSmallException(PageSize, MinPageSize, MaxPageSize);
            if (PageSize > MaxPageSize)
                throw new PageSizeTooBigException(PageSize, MinPageSize, MaxPageSize);
            await QueryValidationHelper.CheckUserExistsAsync(callContext.DbContext, UserId);
            await QueryValidationHelper.CheckCardExistsAsync(callContext.DbContext, CardId);
            CardVisibilityHelper.CheckUserIsAllowedToViewCard(callContext.DbContext, UserId, CardId);
        }
    }
    public sealed record Result(int TotalCount, int PageCount, ImmutableArray<ResultEntry> Entries);
    public sealed record ResultEntry(Guid Id, MemCheckUser Creator, string Text, DateTime CreationUtcDate, bool HasBeenEdited);
    #endregion
}
