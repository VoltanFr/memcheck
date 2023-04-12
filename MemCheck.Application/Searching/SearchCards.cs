using MemCheck.Application.QueryValidation;
using MemCheck.Basics;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Searching;

public sealed class SearchCards : RequestRunner<SearchCards.Request, SearchCards.Result>
{
    #region Field: runDate
    private readonly DateTime runDate;
    #endregion
    public SearchCards(CallContext callContext, DateTime? runDate = null) : base(callContext)
    {
        this.runDate = runDate == null ? DateTime.UtcNow : runDate.Value;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0045:Convert to conditional expression", Justification = "Too complicated")]
    protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
    {
        var allCards = DbContext.Cards.AsNoTracking()
            .Include(card => card.TagsInCards)
            .ThenInclude(tagInCard => tagInCard.Tag)
            .Include(card => card.VersionCreator)
            .Include(card => card.CardLanguage)
            .Include(card => card.UsersWithView)
            .ThenInclude(usersWithView => usersWithView.User)
            .AsSingleQuery();

        var cardsViewableByUser = allCards.Where(card => !card.UsersWithView.Any() || card.UsersWithView.Any(userWithView => userWithView.UserId == request.UserId));

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
            cardsFilteredWithRequiredTags = cardsFilteredWithRequiredTags.Where(card => card.TagsInCards.Any(tagInCard => tagInCard.TagId == tag));

        var cardsFilteredWithExludedTags = cardsFilteredWithRequiredTags;
        if (request.ExcludedTags == null)
            cardsFilteredWithExludedTags = cardsFilteredWithExludedTags.Where(card => !card.TagsInCards.Any());
        else
            foreach (var tag in request.ExcludedTags)   //I tried to do better with an intersect between the two sets, but that failed
                cardsFilteredWithExludedTags = cardsFilteredWithExludedTags.Where(card => !card.TagsInCards.Any(tagInCard => tagInCard.TagId == tag));

        var cardsFilteredWithText = string.IsNullOrEmpty(request.RequiredText) ? cardsFilteredWithExludedTags :
            cardsFilteredWithExludedTags.Where(card =>
                EF.Functions.Like(card.WholeText, $"%{request.RequiredText}%")
            );

        IQueryable<Card> cardsFilteredWithVisibility;
        if (request.Visibility == Request.VibilityFiltering.CardsVisibleByMoreThanOwner)
            cardsFilteredWithVisibility = cardsFilteredWithText.Where(card => card.UsersWithView.Count() != 1);
        else
        {
            cardsFilteredWithVisibility = request.Visibility == Request.VibilityFiltering.PrivateToOwner
                ? cardsFilteredWithText.Where(card => card.UsersWithView.Count() == 1)
                : cardsFilteredWithText;
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
                cardsFilteredWithAverageRating = request.RatingFiltering == Request.RatingFilteringMode.AtLeast
                    ? cardsFilteredWithVisibility.Where(card => card.RatingCount > 0 && card.AverageRating >= request.RatingFilteringValue)
                    : cardsFilteredWithVisibility.Where(card => card.RatingCount > 0 && card.AverageRating <= request.RatingFilteringValue);
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

        IQueryable<Card> cardsFilteredWithReference;
        if (request.Reference == Request.ReferenceFiltering.Ignore)
            cardsFilteredWithReference = cardsFilteredWithNotifications;
        else
            cardsFilteredWithReference = cardsFilteredWithNotifications.Where(card => request.Reference == Request.ReferenceFiltering.None ? card.References.Length == 0 : card.References.Length > 0);

        var allQueryResults = cardsFilteredWithReference;
        allQueryResults = allQueryResults.OrderByDescending(card => card.VersionUtcDate); //For Take() and Skip(), just below, to work, we need to have an order. In future versions we will offer the user some sorting

        var totalNbCards = await allQueryResults.CountAsync();
        var totalPageCount = (int)Math.Ceiling(((double)totalNbCards) / request.PageSize);

        var pageItems = allQueryResults
            .Skip((request.PageNo - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(card => new ResultCard(
                card.Id,
                card.FrontSide,
                card.TagsInCards.Select(tagInCard => tagInCard.Tag.Name).ToImmutableArray(),
                card.UsersWithView.ToList().ToImmutableArray(),
                card.CardInDecks.Where(cardInDeck => cardInDeck.Deck.Owner.Id == request.UserId).Select(cardInDeck => new ResultCardDeckInfo(cardInDeck.DeckId, cardInDeck.Deck.Description, cardInDeck.CurrentHeap, cardInDeck.BiggestHeapReached, cardInDeck.NbTimesInNotLearnedHeap, cardInDeck.AddToDeckUtcTime, cardInDeck.LastLearnUtcTime, cardInDeck.CurrentHeap == 0 || cardInDeck.ExpiryUtcTime <= runDate, cardInDeck.ExpiryUtcTime)).ToImmutableArray(),
                0,
                card.AverageRating,
                card.RatingCount,
                card.VersionCreator,
                card.VersionUtcDate,
                card.VersionDescription
                )
            );

        var pageEntries = await pageItems.ToImmutableArrayAsync();
        var cardIds = pageEntries.Select(card => card.CardId).ToImmutableHashSet();

        // I don't understand why very well, but I get better perf by loading the user ratings separately than in a joint in the big query above
        var allUserRatings = await DbContext.UserCardRatings
            .AsNoTracking()
            .Where(r => r.UserId == request.UserId && cardIds.Contains(r.CardId))
            .Select(r => new { r.CardId, r.Rating })
            .ToImmutableDictionaryAsync(r => r.CardId, r => r.Rating);

        foreach (var card in pageEntries)
            card.CurrentUserRating = allUserRatings.TryGetValue(card.CardId, out var value) ? value : 0;

        var result = new Result(totalNbCards, totalPageCount, pageEntries);
        return new ResultWithMetrologyProperties<Result>(result,
            ("DeckMode", request.Deck == Guid.Empty ? "Ignore" : request.DeckIsInclusive ? "Inclusive" : "Exclusive"),
            ("InHeap", (request.Heap != null).ToString()),
            IntMetric("PageNo", request.PageNo),
            IntMetric("PageSize", request.PageSize),
            IntMetric("RequiredTextLength", request.RequiredText.Length),
            ("VibilityFiltering", request.Visibility.ToString()),
            ("RatingFilteringMode", request.RatingFiltering.ToString()),
            IntMetric("RatingFilteringValue", request.RatingFilteringValue),
            IntMetric("RequiredTagCount", request.RequiredTags.Count()),
            IntMetric("ExcludedTagCount", request.ExcludedTags == null ? 0 : request.ExcludedTags.Count()),
            ("NotificationFiltering", request.Notification.ToString()),
            ("WithMinimumUtcDateOfCard", (request.MinimumUtcDateOfCards != null).ToString()),
            IntMetric("ResultTotalCardCount", result.TotalNbCards),
            IntMetric("ResultPageCount", result.PageCount),
            IntMetric("ResultCardCount", result.Cards.Length)
            );
    }
    #region Request and result classes
    public sealed record Request : IRequest
    {
        public enum ReferenceFiltering { Ignore, None, NotEmpty };
        public enum VibilityFiltering { Ignore, CardsVisibleByMoreThanOwner, PrivateToOwner };
        public enum RatingFilteringMode { Ignore, AtLeast, AtMost, NoRating };
        public enum NotificationFiltering { Ignore, RegisteredCards, NotRegisteredCards };
        public const int MaxPageSize = 500;

        public Guid UserId { get; init; } = Guid.Empty; //Guid.Empty means no user logged in
        public Guid Deck { get; init; } = Guid.Empty; //Guid.Empty means ignore
        public bool DeckIsInclusive { get; init; } = true;   //Makes sense only if Deck is not Guid.Empty
        public int? Heap { get; init; }
        public int PageNo { get; init; } = 1;
        public int PageSize { get; init; } = 10;
        public string RequiredText { get; init; } = "";
        public VibilityFiltering Visibility { get; init; } = VibilityFiltering.Ignore;
        public RatingFilteringMode RatingFiltering { get; init; } = RatingFilteringMode.Ignore;
        public int RatingFilteringValue { get; init; } = 1;
        public IEnumerable<Guid> RequiredTags { get; init; } = Array.Empty<Guid>();
        public IEnumerable<Guid>? ExcludedTags { get; init; } = Array.Empty<Guid>(); //null means that we return only cards which have no tag (we exclude all tags)
        public NotificationFiltering Notification { get; init; } = NotificationFiltering.Ignore;
        public ReferenceFiltering Reference { get; init; } = ReferenceFiltering.Ignore;
        public DateTime? MinimumUtcDateOfCards { get; init; }

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
            if (RequiredText != RequiredText.Trim())
                throw new SearchTextNotTrimmedException("Invalid required text: not trimmed");
        }
    }
    public sealed class Result
    {
        public Result(int totalNbCards, int totalPageCount, ImmutableArray<ResultCard> cards)
        {
            TotalNbCards = totalNbCards;
            PageCount = totalPageCount;
            Cards = cards;
        }
        public int TotalNbCards { get; }
        public int PageCount { get; }
        public ImmutableArray<ResultCard> Cards { get; }
    }
    public sealed class ResultCard
    {
        public ResultCard(Guid cardId, string frontSide, ImmutableArray<string> tags, ImmutableArray<UserWithViewOnCard> visibleTo, ImmutableArray<ResultCardDeckInfo> deckInfo, int currentUserRating, double averageRating, int countOfUserRatings, MemCheckUser versionCreator, DateTime versionUtcDate, string versionDescription)
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
        public ImmutableArray<string> Tags { get; }
        public ImmutableArray<UserWithViewOnCard> VisibleTo { get; }
        public ImmutableArray<ResultCardDeckInfo> DeckInfo { get; }
        public int CurrentUserRating { get; set; }
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
