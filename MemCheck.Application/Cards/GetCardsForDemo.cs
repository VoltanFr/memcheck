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

namespace MemCheck.Application.Cards
{
    /* Returns a set of cards for demo mode.
     * We take a set of 3 times the number of requested cards, sorted by rating, then we shuffle this set. */
    public sealed class GetCardsForDemo : RequestRunner<GetCardsForDemo.Request, GetCardsForDemo.Result>
    {
        #region Fields
        private readonly DateTime? now;
        #endregion
        #region Private method
        private async Task<IEnumerable<ResultCard>> GetUnknownCardsAsync(Guid tagId, IEnumerable<Guid> excludedCardIds, ImmutableDictionary<Guid, string> userNames, ImmutableDictionary<Guid, string> imageNames, ImmutableDictionary<Guid, string> tagNames, int cardCount)
        {
            var cardsWithTag = DbContext.TagsInCards
                .AsNoTracking()
                .Include(tagInCard => tagInCard.Card)
                .AsSingleQuery()
                .Where(tagInCard => tagInCard.TagId == tagId && !excludedCardIds.Contains(tagInCard.CardId))
                .Where(tagInCard => !tagInCard.Card.UsersWithView.Any())
                .OrderByDescending(tagInCard => tagInCard.Card.AverageRating)
                .Take(cardCount * 3);

            var result = await cardsWithTag.Select(cardInDeck => new ResultCard(
                 cardInDeck.CardId,
                 cardInDeck.Card.FrontSide,
                 cardInDeck.Card.BackSide,
                 cardInDeck.Card.AdditionalInfo,
                 cardInDeck.Card.References,
                 cardInDeck.Card.VersionUtcDate,
                 userNames[cardInDeck.Card.VersionCreator.Id],
                 cardInDeck.Card.TagsInCards.Select(tag => tagNames[tag.TagId]),
                 cardInDeck.Card.Images.Select(img => new ResultImageModel(img.ImageId, imageNames[img.ImageId], img.CardSide)),
                 cardInDeck.Card.AverageRating,
                 cardInDeck.Card.RatingCount,
                 cardInDeck.Card.CardLanguage.Name == "Français" //Questionable hardcoding
             )).ToListAsync();

            if (result.Count > cardCount)
            {
                var highestRatingToReturn = result[0].AverageRating;
                var lowestRatingToReturn = result[cardCount - 1].AverageRating;
                if (highestRatingToReturn != lowestRatingToReturn)
                    result = result.Take(cardCount).ToList();
            }

            return Shuffler.Shuffle(result).Take(cardCount);
        }
        #endregion
        public GetCardsForDemo(CallContext callContext, DateTime? now = null) : base(callContext)
        {
            this.now = now;
        }
        protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
        {
            var userNames = DbContext.Users.AsNoTracking().Select(u => new { u.Id, u.UserName }).ToImmutableDictionary(u => u.Id, u => u.UserName);
            var imageNames = ImageLoadingHelper.GetAllImageNames(DbContext);
            var tagNames = TagLoadingHelper.Run(DbContext);

            var result = new List<ResultCard>();
            result.AddRange(await GetUnknownCardsAsync(request.TagId, request.ExcludedCardIds, userNames, imageNames, tagNames, request.CardsToDownload));

            var auditEntry = new DemoDownloadAuditTrailEntry()
            {
                TagId = request.TagId,
                DownloadUtcDate = now == null ? DateTime.UtcNow : now.Value,
                CountOfCardsReturned = result.Count
            };
            DbContext.DemoDownloadAuditTrailEntries.Add(auditEntry);
            await DbContext.SaveChangesAsync();

            return new ResultWithMetrologyProperties<Result>(new Result(result),
                ("TagId", request.TagId.ToString()),
               IntMetric("ExcludedCardCount", request.ExcludedCardIds.Count()),
               IntMetric("RequestedCardCount", request.CardsToDownload),
               IntMetric("ResultCount", result.Count));
        }
        #region Request and Result
        public sealed class Request : IRequest
        {
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
                if (CardsToDownload is < 1 or > 100)
                    throw new RequestInputException($"Invalid CardsToDownload: {CardsToDownload}");
            }
        }
        public sealed record Result(IEnumerable<ResultCard> Cards);
        public sealed class ResultCard
        {
            public ResultCard(Guid cardId, string frontSide, string backSide, string additionalInfo, string references, DateTime lastChangeUtcTime, string versionCreator, IEnumerable<string> tags,
                IEnumerable<ResultImageModel> images, double averageRating, int countOfUserRatings, bool isInFrench)
            {
                CardId = cardId;
                LastChangeUtcTime = lastChangeUtcTime;
                VersionCreator = versionCreator;
                FrontSide = frontSide;
                BackSide = backSide;
                AdditionalInfo = additionalInfo;
                References = references;
                Tags = tags;
                Images = images;
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
            public IEnumerable<ResultImageModel> Images { get; }
        }
        public sealed class ResultImageModel
        {
            public ResultImageModel(Guid id, string name, int cardSide)
            {
                ImageId = id;
                Name = name;
                CardSide = cardSide;
            }
            public Guid ImageId { get; }
            public string Name { get; }
            public int CardSide { get; set; }   //1 = front side ; 2 = back side ; 3 = AdditionalInfo, but use the constants in ImageInCard
        }
        #endregion
    }
}