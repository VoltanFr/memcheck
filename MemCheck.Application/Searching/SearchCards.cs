using MemCheck.Application.QueryValidation;
using MemCheck.Basics;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Searching
{
    public sealed class SearchCards : RequestRunner<SearchCards.Request, SearchCards.Result>
    {
        #region Private class ResultCardBeforeDeckInfo
        private sealed class ResultCardBeforeDeckInfo
        {
            public ResultCardBeforeDeckInfo(Guid cardId, string frontSide, IEnumerable<string> tags, IEnumerable<UserWithViewOnCard> visibleTo, MemCheckUser versionCreator, DateTime versionUtcDate, string versionDescription, int userRating, double averageRating, int ratingCount)
            {
                CardId = cardId;
                FrontSide = frontSide;
                Tags = tags;
                VisibleTo = visibleTo;

                CurrentUserRating = userRating;
                AverageRating = averageRating;
                CountOfUserRatings = ratingCount;
                VersionCreator = versionCreator;
                VersionUtcDate = versionUtcDate;
                VersionDescription = versionDescription;
            }
            public Guid CardId { get; }
            public string FrontSide { get; }
            public IEnumerable<string> Tags { get; }
            public IEnumerable<UserWithViewOnCard> VisibleTo { get; }
            public int CurrentUserRating { get; }
            public double AverageRating { get; }
            public int CountOfUserRatings { get; }
            public MemCheckUser VersionCreator { get; }
            public DateTime VersionUtcDate { get; }
            public string VersionDescription { get; }
        }
        #endregion
        #region private methods
        private async Task<ImmutableArray<ResultCardDeckInfo>> GetCardDeckInfoAsync(ResultCardBeforeDeckInfo card, Guid userId)
        {
            var cardsInDecksForThisUserAndThisCard = await DbContext.CardsInDecks
                .AsNoTracking()
                .Include(card => card.Deck)
                .Where(cardInDeck => cardInDeck.Deck.Owner.Id == userId && cardInDeck.CardId == card.CardId)
                .ToArrayAsync();

            var result = cardsInDecksForThisUserAndThisCard.Select(cardInDeck => new ResultCardDeckInfo(
                cardInDeck.DeckId,
                cardInDeck.Deck.Description,
                cardInDeck.CurrentHeap,
                cardInDeck.BiggestHeapReached,
                cardInDeck.NbTimesInNotLearnedHeap,
                cardInDeck.AddToDeckUtcTime,
                cardInDeck.LastLearnUtcTime,
                cardInDeck.CurrentHeap == 0 || cardInDeck.ExpiryUtcTime <= DateTime.UtcNow,
                cardInDeck.ExpiryUtcTime));

            return result.ToImmutableArray();
        }
        private async Task<ImmutableArray<ResultCard>> AddDeckInfosAsync(Guid userId, ImmutableArray<ResultCardBeforeDeckInfo> resultCards)
        {
            var cardDeckInfos = new Dictionary<Guid, IEnumerable<ResultCardDeckInfo>>();
            foreach (var card in resultCards)
            {
                var deckInfo = await GetCardDeckInfoAsync(card, userId);
                cardDeckInfos.Add(card.CardId, deckInfo);
            }

            var result = resultCards.Select(card => new ResultCard(
                card.CardId,
                card.FrontSide,
                card.Tags,
                card.VisibleTo,
                cardDeckInfos[card.CardId],
                card.CurrentUserRating,
                card.AverageRating,
                card.CountOfUserRatings,
                card.VersionCreator,
                card.VersionUtcDate,
                card.VersionDescription)
            );

            return result.ToImmutableArray();
        }
        private async Task<ImmutableArray<ResultCardBeforeDeckInfo>> AddUserRatings(Guid userId, ImmutableArray<Card> cards)
        {
            var allUserRatings = (await DbContext.UserCardRatings.Where(r => r.UserId == userId).Select(r => new { r.CardId, r.Rating }).ToDictionaryAsync(r => r.CardId, r => r.Rating)).ToImmutableDictionary();

            var resultCards = cards.Select(card => new ResultCardBeforeDeckInfo(
                card.Id,
                card.FrontSide,
                card.TagsInCards.Select(tagInCard => tagInCard.Tag.Name),
                card.UsersWithView,
                card.VersionCreator,
                card.VersionUtcDate,
                card.VersionDescription,
                allUserRatings.ContainsKey(card.Id) ? allUserRatings[card.Id] : 0,
                card.AverageRating,
                card.RatingCount
                )
            );

            return resultCards.ToImmutableArray();
        }
        #endregion
        public SearchCards(CallContext callContext) : base(callContext)
        {
        }
        protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
        {
            var allCards = DbContext.Cards.AsNoTracking()
                .Include(card => card.TagsInCards)
                .ThenInclude(tagInCard => tagInCard.Tag)
                .Include(card => card.VersionCreator)
                .Include(card => card.CardLanguage)
                .Include(card => card.UsersWithView)
                .AsSingleQuery();

            var cardsViewableByUser = allCards.Where(card => !card.UsersWithView.Any() || card.UsersWithView.Where(userWithView => userWithView.UserId == request.UserId).Any());

            var cardsFilteredWithDate = request.MinimumUtcDateOfCards == null ? cardsViewableByUser : cardsViewableByUser.Where(card => card.VersionUtcDate >= request.MinimumUtcDateOfCards.Value);

            IQueryable<Card> cardsFilteredWithDeck;
            if (request.Deck != Guid.Empty)
            {
                if (request.DeckIsInclusive)
                    cardsFilteredWithDeck = cardsFilteredWithDate.Where(card =>
                        DbContext.CardsInDecks.AsNoTracking().Where(cardInDeck => cardInDeck.CardId == card.Id && cardInDeck.DeckId == request.Deck).Any()
                        && (request.Heap == null || DbContext.CardsInDecks.AsNoTracking().Single(cardInDeck => cardInDeck.CardId == card.Id && cardInDeck.DeckId == request.Deck).CurrentHeap == request.Heap.Value)
                        );
                else
                    cardsFilteredWithDeck = cardsFilteredWithDate.Where(card => !DbContext.CardsInDecks.AsNoTracking().Where(cardInDeck => cardInDeck.CardId == card.Id && cardInDeck.DeckId == request.Deck).Any());
            }
            else
                cardsFilteredWithDeck = cardsFilteredWithDate;

            var cardsFilteredWithRequiredTags = cardsFilteredWithDeck;
            foreach (var tag in request.RequiredTags)   //I tried to do better with an intersect between the two sets, but that failed
                cardsFilteredWithRequiredTags = cardsFilteredWithRequiredTags.Where(card => card.TagsInCards.Where(tagInCard => tagInCard.TagId == tag).Any());

            var cardsFilteredWithExludedTags = cardsFilteredWithRequiredTags;
            if (request.ExcludedTags == null)
                cardsFilteredWithExludedTags = cardsFilteredWithExludedTags.Where(card => !card.TagsInCards.Any());
            else
                foreach (var tag in request.ExcludedTags)   //I tried to do better with an intersect between the two sets, but that failed
                    cardsFilteredWithExludedTags = cardsFilteredWithExludedTags.Where(card => !card.TagsInCards.Where(tagInCard => tagInCard.TagId == tag).Any());

            var cardsFilteredWithText = string.IsNullOrEmpty(request.RequiredText) ? cardsFilteredWithExludedTags :
                cardsFilteredWithExludedTags.Where(card =>
                    EF.Functions.Like(card.FrontSide, $"%{request.RequiredText}%")
                    || EF.Functions.Like(card.BackSide, $"%{request.RequiredText}%")
                    || EF.Functions.Like(card.AdditionalInfo, $"%{request.RequiredText}%")
                    || EF.Functions.Like(card.References, $"%{request.RequiredText}%")
                );

            IQueryable<Card> cardsFilteredWithVisibility;
            if (request.Visibility == Request.VibilityFiltering.CardsVisibleByMoreThanOwner)
                cardsFilteredWithVisibility = cardsFilteredWithText.Where(card => card.UsersWithView.Count() != 1);
            else
            {
                if (request.Visibility == Request.VibilityFiltering.PrivateToOwner)
                    cardsFilteredWithVisibility = cardsFilteredWithText.Where(card => card.UsersWithView.Count() == 1);
                else
                    cardsFilteredWithVisibility = cardsFilteredWithText;
            }

            IQueryable<Card> cardsFilteredWithAverageRating;
            if (request.RatingFiltering == Request.RatingFilteringMode.Ignore)
                cardsFilteredWithAverageRating = cardsFilteredWithVisibility;
            else
            {
                if (request.RatingFiltering == Request.RatingFilteringMode.NoRating)
                    cardsFilteredWithAverageRating = cardsFilteredWithVisibility.Where(card => card.RatingCount == 0);
                else
                {
                    if (request.RatingFiltering == Request.RatingFilteringMode.AtLeast)
                        cardsFilteredWithAverageRating = cardsFilteredWithVisibility.Where(card => card.RatingCount > 0 && card.AverageRating >= request.RatingFilteringValue);
                    else
                        cardsFilteredWithAverageRating = cardsFilteredWithVisibility.Where(card => card.RatingCount > 0 && card.AverageRating <= request.RatingFilteringValue);
                }
            }

            IQueryable<Card> cardsFilteredWithNotifications;
            if (request.Notification == Request.NotificationFiltering.Ignore)
                cardsFilteredWithNotifications = cardsFilteredWithAverageRating;
            else
            {
                var notifMustExist = request.Notification == Request.NotificationFiltering.RegisteredCards;
                cardsFilteredWithNotifications = cardsFilteredWithAverageRating.Where(card => DbContext.CardNotifications.AsNoTracking().Where(cardNotif => cardNotif.CardId == card.Id && cardNotif.UserId == request.UserId).Any() == notifMustExist);
            }

            var finalResult = cardsFilteredWithNotifications;
            finalResult = finalResult.OrderByDescending(card => card.VersionUtcDate); //For Take() and Skip(), just below, to work, we need to have an order. In future versions we will offer the user some sorting

            var totalNbCards = finalResult.Count();
            var totalPageCount = (int)Math.Ceiling(((double)totalNbCards) / request.PageSize);

            var pageCards = (await finalResult.Skip((request.PageNo - 1) * request.PageSize).Take(request.PageSize).ToListAsync()).ToImmutableArray();

            var resultCards = await AddUserRatings(request.UserId, pageCards);
            var withUserDeckInfo = await AddDeckInfosAsync(request.UserId, resultCards);

            var result = new Result(totalNbCards, totalPageCount, withUserDeckInfo);
            return new ResultWithMetrologyProperties<Result>(result,
                ("DeckMode", request.Deck == Guid.Empty ? "Ignore" : request.DeckIsInclusive ? "Inclusive" : "Exclusive"),
                ("InHeap", (request.Heap != null).ToString()),
                ("PageNo", request.PageNo.ToString()),
                ("PageSize", request.PageSize.ToString()),
                ("RequiredTextLength", request.RequiredText.Length.ToString()),
                ("VibilityFiltering", request.Visibility.ToString()),
                ("RatingFilteringMode", request.RatingFiltering.ToString()),
                ("RatingFilteringValue", request.RatingFilteringValue.ToString()),
                ("RequiredTagCount", request.RequiredTags.Count().ToString()),
                ("ExcludedTagCount", request.ExcludedTags == null ? "All" : request.ExcludedTags.Count().ToString()),
                ("NotificationFiltering", request.Notification.ToString()),
                ("WithMinimumUtcDateOfCard", (request.MinimumUtcDateOfCards != null).ToString()),
                ("ResultTotalCardCount", result.TotalNbCards.ToString()),
                ("ResultPageCount", result.PageCount.ToString()),
                ("ResultCardCount", result.Cards.Count().ToString())
                );
        }
        #region Request and result classes
        public sealed record Request : IRequest
        {
            public enum VibilityFiltering { Ignore, CardsVisibleByMoreThanOwner, PrivateToOwner };
            public enum RatingFilteringMode { Ignore, AtLeast, AtMost, NoRating };
            public enum NotificationFiltering { Ignore, RegisteredCards, NotRegisteredCards };
            public const int MaxPageSize = 500;

            public Guid UserId { get; init; } = Guid.Empty; //Guid.Empty means no user logged in
            public Guid Deck { get; init; } = Guid.Empty; //Guid.Empty means ignore
            public bool DeckIsInclusive { get; init; } = true;   //Makes sense only if Deck is not Guid.Empty
            public int? Heap { get; init; } = null;
            public int PageNo { get; init; } = 1;
            public int PageSize { get; init; } = 10;
            public string RequiredText { get; init; } = "";
            public VibilityFiltering Visibility { get; init; } = VibilityFiltering.Ignore;
            public RatingFilteringMode RatingFiltering { get; init; } = RatingFilteringMode.Ignore;
            public int RatingFilteringValue { get; init; } = 1;
            public IEnumerable<Guid> RequiredTags { get; init; } = Array.Empty<Guid>();
            public IEnumerable<Guid>? ExcludedTags { get; init; } = Array.Empty<Guid>(); //null means that we return only cards which have no tag (we exclude all tags)
            public NotificationFiltering Notification { get; init; } = NotificationFiltering.Ignore;
            public DateTime? MinimumUtcDateOfCards { get; init; } = null;

            public async Task CheckValidityAsync(CallContext callContext)
            {
                if (Heap != null && (Heap.Value < 0 || Heap.Value > CardInDeck.MaxHeapValue))
                    throw new RequestInputException($"Invalid heap {Heap}");
                if (PageNo < 1)
                    throw new RequestInputException($"First page is numbered 1, received a request for page {PageNo}");
                if (PageSize > MaxPageSize)
                    throw new RequestInputException($"PageSize too big: {PageSize} (max size: {MaxPageSize})");
                if ((RatingFiltering == RatingFilteringMode.AtLeast || RatingFiltering == RatingFilteringMode.AtMost) && (RatingFilteringValue < 1 || RatingFilteringValue > 5))
                    throw new RequestInputException($"Invalid RatingFilteringValue: {RatingFilteringValue}");
                if (UserId == Guid.Empty && Deck != Guid.Empty)
                    throw new RequestInputException("Can not search a deck if not logged in");
                if (UserId != Guid.Empty)
                {
                    QueryValidationHelper.CheckNotReservedGuid(UserId);
                    if (Deck != Guid.Empty)
                        await QueryValidationHelper.CheckUserIsOwnerOfDeckAsync(callContext.DbContext, UserId, Deck);
                }
            }
        }
        public sealed class Result
        {
            public Result(int totalNbCards, int totalPageCount, IEnumerable<ResultCard> cards)
            {
                TotalNbCards = totalNbCards;
                PageCount = totalPageCount;
                Cards = cards;
            }
            public int TotalNbCards { get; }
            public int PageCount { get; }
            public IEnumerable<ResultCard> Cards { get; }
        }
        public sealed class ResultCard
        {
            public ResultCard(Guid cardId, string frontSide, IEnumerable<string> tags, IEnumerable<UserWithViewOnCard> visibleTo, IEnumerable<ResultCardDeckInfo> deckInfo, int currentUserRating, double averageRating, int countOfUserRatings, MemCheckUser versionCreator, DateTime versionUtcDate, string versionDescription)
            {
                CardId = cardId;
                FrontSide = frontSide.Truncate(150);
                Tags = tags;
                VisibleTo = visibleTo;
                DeckInfo = deckInfo;
                CurrentUserRating = currentUserRating;
                AverageRating = averageRating;
                CountOfUserRatings = countOfUserRatings;
                VersionCreator = versionCreator;
                VersionUtcDate = versionUtcDate;
                VersionDescription = versionDescription;
            }
            public Guid CardId { get; }
            public string FrontSide { get; }
            public IEnumerable<string> Tags { get; }
            public IEnumerable<UserWithViewOnCard> VisibleTo { get; }
            public IEnumerable<ResultCardDeckInfo> DeckInfo { get; }
            public int CurrentUserRating { get; }
            public double AverageRating { get; }    //0 if no rating
            public int CountOfUserRatings { get; }
            public MemCheckUser VersionCreator { get; }
            public DateTime VersionUtcDate { get; }
            public string VersionDescription { get; }
        }
        public sealed class ResultCardDeckInfo
        {
            public ResultCardDeckInfo(Guid deckId, string deckName, int currentHeap, int biggestHeapReached, int nbTimesInNotLearnedHeap, DateTime addToDeckUtcTime, DateTime lastLearnUtcTime, bool expired, DateTime expiryUtcDate)
            {
                DeckId = deckId;
                DeckName = deckName;
                CurrentHeap = currentHeap;
                BiggestHeapReached = biggestHeapReached;
                NbTimesInNotLearnedHeap = nbTimesInNotLearnedHeap;
                AddToDeckUtcTime = addToDeckUtcTime;
                LastLearnUtcTime = lastLearnUtcTime;
                Expired = expired;
                ExpiryUtcDate = expiryUtcDate;
            }
            public Guid DeckId { get; }
            public string DeckName { get; }
            public int CurrentHeap { get; }
            public int BiggestHeapReached { get; }
            public int NbTimesInNotLearnedHeap { get; }
            public DateTime AddToDeckUtcTime { get; }
            public DateTime LastLearnUtcTime { get; }
            public bool Expired { get; }
            public DateTime ExpiryUtcDate { get; }
        }
        #endregion
    }
}