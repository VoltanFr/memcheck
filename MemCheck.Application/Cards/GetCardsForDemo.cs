using MemCheck.Application.Images;
using MemCheck.Application.QueryValidation;
using MemCheck.Application.Tags;
using MemCheck.Basics;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards;

/* Returns a set of public cards for demo mode.
 * We take a big set of cards, and split this set into buckets per rating.
 * We shuffle each bucket contents.
 * We merge again the buckets, in order. */
public sealed class GetCardsForDemo : RequestRunner<GetCardsForDemo.Request, GetCardsForDemo.Result>
{
    #region Fields
    private readonly DateTime? now;
    #endregion
    #region Private method
    private async Task<ImmutableArray<ResultCard>> GetCardsAsync(Guid tagId, IEnumerable<Guid> excludedCardIds, ImmutableDictionary<Guid, string> userNames, ImmutableDictionary<Guid, string> tagNames, int cardCount)
    {
        var cards = await DbContext.TagsInCards
            .AsNoTracking()
            .Include(tagInCard => tagInCard.Card)
            .Include(tagInCard => tagInCard.Card.VersionCreator)
            .Include(tagInCard => tagInCard.Card.CardLanguage)
            .Include(tagInCard => tagInCard.Card.TagsInCards)
            .Where(tagInCard => tagInCard.TagId == tagId && !excludedCardIds.Contains(tagInCard.CardId))
            .Where(tagInCard => !tagInCard.Card.UsersWithView.Any())
            .OrderByDescending(tagInCard => tagInCard.Card.AverageRating)
            .Take(Request.MaxCount * 10)
            .Select(tagInCard => new ResultCard(
                tagInCard.Card.Id,
                tagInCard.Card.FrontSide,
                tagInCard.Card.BackSide,
                tagInCard.Card.AdditionalInfo,
                tagInCard.Card.References,
                tagInCard.Card.VersionUtcDate,
                userNames[tagInCard.Card.VersionCreator.Id],
                tagInCard.Card.TagsInCards.Select(tag => tagNames[tag.TagId]),
                tagInCard.Card.AverageRating,
                tagInCard.Card.RatingCount,
                tagInCard.Card.CardLanguage.Name == "Français" // Questionable hardcoding
            ))
            .ToListAsync();

        var groups = cards.GroupBy(card => Math.Truncate(card.AverageRating));
        var result = new List<ResultCard>();

        foreach (var group in groups.OrderByDescending(group => group.Key))
            result.AddRange(Shuffler.Shuffle(group));

        return result.Take(cardCount).ToImmutableArray();
    }
    #endregion
    public GetCardsForDemo(CallContext callContext, DateTime? now = null) : base(callContext)
    {
        this.now = now;
    }
    protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
    {
        var userNames = DbContext.Users.AsNoTracking().Select(u => new { u.Id, UserName = u.GetUserName() }).ToImmutableDictionary(u => u.Id, u => u.UserName);
        var imageNames = ImageLoadingHelper.GetAllImageNames(DbContext);
        var tagNames = TagLoadingHelper.Run(DbContext);

        var result = await GetCardsAsync(request.TagId, request.ExcludedCardIds, userNames, tagNames, request.CardsToDownload);

        var auditEntry = new DemoDownloadAuditTrailEntry()
        {
            TagId = request.TagId,
            DownloadUtcDate = now == null ? DateTime.UtcNow : now.Value,
            CountOfCardsReturned = result.Length
        };
        DbContext.DemoDownloadAuditTrailEntries.Add(auditEntry);
        await DbContext.SaveChangesAsync();

        return new ResultWithMetrologyProperties<Result>(new Result(result),
           GuidMetric("TagId", request.TagId),
           IntMetric("ExcludedCardCount", request.ExcludedCardIds.Count()),
           IntMetric("RequestedCardCount", request.CardsToDownload),
           IntMetric("ResultCount", result.Length));
    }
    #region Request and Result
    public sealed class Request : IRequest
    {
        public const int MaxCount = 100;
        public Request(Guid tagId, IEnumerable<Guid> excludedCardIds, int cardsToDownload)
        {
            TagId = tagId;
            ExcludedCardIds = excludedCardIds;
            CardsToDownload = cardsToDownload;
        }
        public Guid TagId { get; }
        public IEnumerable<Guid> ExcludedCardIds { get; }
        public int CardsToDownload { get; }
        public async Task CheckValidityAsync(CallContext callContext)
        {
            if (QueryValidationHelper.IsReservedGuid(TagId))
                throw new RequestInputException($"Invalid tag id '{TagId}'");
            await QueryValidationHelper.CheckTagExistsAsync(TagId, callContext.DbContext);
            if (ExcludedCardIds.Any(cardId => QueryValidationHelper.IsReservedGuid(cardId)))
                throw new RequestInputException($"Invalid card id");
            if (CardsToDownload is < 1 or > MaxCount)
                throw new RequestInputException($"Invalid CardsToDownload: {CardsToDownload}");
        }
    }
    public sealed record Result(IEnumerable<ResultCard> Cards);
    public sealed class ResultCard
    {
        public ResultCard(Guid cardId, string frontSide, string backSide, string additionalInfo, string references, DateTime lastChangeUtcTime, string versionCreator, IEnumerable<string> tags, double averageRating, int countOfUserRatings, bool isInFrench)
        {
            CardId = cardId;
            LastChangeUtcTime = lastChangeUtcTime;
            VersionCreator = versionCreator;
            FrontSide = frontSide;
            BackSide = backSide;
            AdditionalInfo = additionalInfo;
            References = references;
            Tags = tags;
            AverageRating = averageRating;
            CountOfUserRatings = countOfUserRatings;
            IsInFrench = isInFrench;
        }
        public Guid CardId { get; }
        public DateTime LastChangeUtcTime { get; }
        public string FrontSide { get; }
        public string BackSide { get; }
        public string AdditionalInfo { get; }
        public string References { get; }
        public string VersionCreator { get; }
        public double AverageRating { get; }
        public int CountOfUserRatings { get; }
        public bool IsInFrench { get; }
        public IEnumerable<string> Tags { get; }
    }
    #endregion
}
