﻿using MemCheck.Application;
using MemCheck.Application.Cards;
using MemCheck.Application.Decks;
using MemCheck.Application.Heaping;
using MemCheck.Application.Notifiying;
using MemCheck.Application.QueryValidation;
using MemCheck.Application.Searching;
using MemCheck.Application.Tags;
using MemCheck.Application.Users;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.WebUI.Controllers;

[Route("[controller]")]
public class SearchController : MemCheckController
{
    #region Fields
    private readonly CallContext callContext;
    private readonly UserManager<MemCheckUser> userManager;
    private static readonly Guid noTagFakeGuid = Guid.Empty;
    private static readonly Guid allTagsFakeGuid = new("11111111-1111-1111-1111-111111111111");
    #endregion
    public SearchController(MemCheckDbContext dbContext, UserManager<MemCheckUser> userManager, IStringLocalizer<SearchController> localizer, TelemetryClient telemetryClient) : base(localizer)
    {
        callContext = new CallContext(dbContext, new MemCheckTelemetryClient(telemetryClient), this, new ProdRoleChecker(userManager));
        this.userManager = userManager;
    }
    #region GetAllStaticData
    [HttpGet("GetAllStaticData")]
    public async Task<IActionResult> GetAllStaticData()
    {
        var user = await userManager.GetUserAsync(HttpContext.User);
        var decksWithHeapsAndTags = user == null
            ? Array.Empty<GetUserDecksWithHeapsAndTags.Result>()
            : await new GetUserDecksWithHeapsAndTags(callContext).RunAsync(new GetUserDecksWithHeapsAndTags.Request(user.Id));
        var allTags = await new GetAllTags(callContext).RunAsync(new GetAllTags.Request(GetAllTags.Request.MaxPageSize, 1, ""));
        var allUsers = await new GetUsers(callContext).RunAsync(new GetUsers.Request());
        GetAllStaticDataViewModel value = new(decksWithHeapsAndTags, allTags.Tags, allUsers, this, user);
        return base.Ok(value);
    }
    #region View model classes
    public sealed class GetAllStaticDataViewModel
    {
        public GetAllStaticDataViewModel(IEnumerable<GetUserDecksWithHeapsAndTags.Result> userDecks, IEnumerable<GetAllTags.ResultTag> allTags, IEnumerable<GetUsers.ViewModel> allUsers, ILocalized localizer, MemCheckUser? currentUser)
        {
            UserDecks = new[] { new GetAllStaticDataDeckViewModel(Guid.Empty, localizer.GetLocalized("Ignore")) }
                .Concat(userDecks.Select(applicationResult => new GetAllStaticDataDeckViewModel(applicationResult, localizer)));
            AllDecksForAddingCards = userDecks.Select(applicationResult => new GetAllStaticDataDeckForAddViewModel(applicationResult.DeckId, applicationResult.Description));
            AllApplicableTags = allTags.Select(tag => new GetAllStaticDataTagViewModel(tag.TagId, tag.TagName));
            AllRequirableTags = new[] { new GetAllStaticDataTagViewModel(noTagFakeGuid, localizer.GetLocalized("None")) }
                .Concat(allTags.Select(tag => new GetAllStaticDataTagViewModel(tag.TagId, tag.TagName)));
            AllExcludableTags = new[] {
                new GetAllStaticDataTagViewModel(noTagFakeGuid, localizer.GetLocalized("None")),
                new GetAllStaticDataTagViewModel(allTagsFakeGuid, localizer.GetLocalized("All"))
                }
                .Concat(allTags.Select(tag => new GetAllStaticDataTagViewModel(tag.TagId, tag.TagName)));
            AllUsers = new[] { new GetAllStaticDataUserViewModel(Guid.Empty, localizer.GetLocalized("Any")) }
                .Concat(allUsers.Select(user => new GetAllStaticDataUserViewModel(user.UserId, user.UserName)));
            LocalizedText = new GetAllStaticDataLocalizedTextViewModel(localizer);
            PossibleHeapsForMove = Enumerable.Range(0, CardInDeck.MaxHeapValue).Select(heapId => new GetAllStaticDataHeapViewModel(heapId, DisplayServices.HeapName(heapId, localizer)));
            CurrentUserId = currentUser == null ? Guid.Empty : currentUser.Id;
            ShowDebugInfo = DisplayServices.ShowDebugInfo(currentUser);
        }
        public IEnumerable<GetAllStaticDataDeckViewModel> UserDecks { get; }
        public IEnumerable<GetAllStaticDataTagViewModel> AllApplicableTags { get; }
        public IEnumerable<GetAllStaticDataTagViewModel> AllRequirableTags { get; }
        public IEnumerable<GetAllStaticDataTagViewModel> AllExcludableTags { get; }
        public IEnumerable<GetAllStaticDataUserViewModel> AllUsers { get; }
        public GetAllStaticDataLocalizedTextViewModel LocalizedText { get; }
        public IEnumerable<GetAllStaticDataDeckForAddViewModel> AllDecksForAddingCards { get; }
        public IEnumerable<GetAllStaticDataHeapViewModel> PossibleHeapsForMove { get; }
        public Guid CurrentUserId { get; }
        public bool ShowDebugInfo { get; }
    }
    public sealed class GetAllStaticDataDeckViewModel
    {
        public GetAllStaticDataDeckViewModel(GetUserDecksWithHeapsAndTags.Result applicationResult, ILocalized localizer)
        {
            DeckId = applicationResult.DeckId;
            DeckName = applicationResult.Description;
            Heaps = new[] { new GetAllStaticDataHeapViewModel(-1, localizer.GetLocalized("Any")) }
                .Concat(applicationResult.Heaps.OrderBy(heap => heap).Select(heap => new GetAllStaticDataHeapViewModel(heap, DisplayServices.HeapName(heap, localizer))));
            RequirableTags = new[] { new GetAllStaticDataTagViewModel(noTagFakeGuid, localizer.GetLocalized("None")) }
                .Concat(applicationResult.Tags.Select(tag => new GetAllStaticDataTagViewModel(tag.TagId, tag.TagName)));
            ExcludableTags = new[] {
                new GetAllStaticDataTagViewModel(noTagFakeGuid, localizer.GetLocalized("None")),
                new GetAllStaticDataTagViewModel(allTagsFakeGuid, localizer.GetLocalized("All"))
                }
                .Concat(applicationResult.Tags.Select(tag => new GetAllStaticDataTagViewModel(tag.TagId, tag.TagName)));
        }
        public GetAllStaticDataDeckViewModel(Guid deckId, string deckName)
        {
            DeckId = deckId;
            DeckName = deckName;
            Heaps = Array.Empty<GetAllStaticDataHeapViewModel>();
            RequirableTags = Array.Empty<GetAllStaticDataTagViewModel>();
            ExcludableTags = Array.Empty<GetAllStaticDataTagViewModel>();
        }
        public Guid DeckId { get; }
        public string DeckName { get; }
        public IEnumerable<GetAllStaticDataHeapViewModel> Heaps { get; }
        public IEnumerable<GetAllStaticDataTagViewModel> RequirableTags { get; }
        public IEnumerable<GetAllStaticDataTagViewModel> ExcludableTags { get; }
    }
    public sealed class GetAllStaticDataDeckForAddViewModel
    {
        public GetAllStaticDataDeckForAddViewModel(Guid deckId, string deckName)
        {
            DeckId = deckId;
            DeckName = deckName;
        }
        public Guid DeckId { get; }
        public string DeckName { get; }
    }
    public sealed class GetAllStaticDataHeapViewModel
    {
        public GetAllStaticDataHeapViewModel(int heapId, string heapName)
        {
            HeapId = heapId;
            HeapName = heapName;
        }
        public int HeapId { get; }
        public string HeapName { get; }
    }
    public sealed class GetAllStaticDataTagViewModel
    {
        public GetAllStaticDataTagViewModel(Guid tagId, string tagName)
        {
            TagId = tagId;
            TagName = tagName;
        }
        public Guid TagId { get; }
        public string TagName { get; }
    }
    public sealed class GetAllStaticDataUserViewModel
    {
        public GetAllStaticDataUserViewModel(Guid userId, string userName)
        {
            UserId = userId;
            UserName = userName;
        }

        public Guid UserId { get; }
        public string UserName { get; }
    }
    public sealed class GetAllStaticDataLocalizedTextViewModel
    {
        public GetAllStaticDataLocalizedTextViewModel(ILocalized localizer)
        {
            Any = localizer.GetLocalized("Any");
            Ignore = localizer.GetLocalized("Ignore");
            Before = localizer.GetLocalized("Before");
            ThatDay = localizer.GetLocalized("On");
            After = localizer.GetLocalized("After");
            CardVisibleToMoreThanOwner = localizer.GetLocalized("CardVisibleToMoreThanOwner");
            CardVisibleToOwnerOnly = localizer.GetLocalized("CardVisibleToOwnerOnly");
            IncludeCards = localizer.GetLocalized("IncludeCards");
            ExcludeCards = localizer.GetLocalized("ExcludeCards");
            OperationIsForSelectedCards = localizer.GetLocalized("OperationIsForSelectedCards");
            TooManySelectedCards = localizer.GetLocalized("TooManySelectedCards");
            AlertAddTagToCardsPart1 = localizer.GetLocalized("AlertAddTagToCardsPart1");
            AlertAddTagToCardsPart2 = localizer.GetLocalized("AlertAddTagToCardsPart2");
            AlertAddTagToCardsPart3Single = localizer.GetLocalized("AlertAddTagToCardsPart3Single");
            AlertAddTagToCardsPart3Plural = localizer.GetLocalized("AlertAddTagToCardsPart3Plural");
            CantAddToDeckBecauseSomeCardsAlreadyIn = localizer.GetLocalized("CantAddToDeckBecauseSomeCardsAlreadyIn");
            AlertAddCardsToDeckPart1 = localizer.GetLocalized("AlertAddCardsToDeckPart1");
            AlertAddCardToDeckPart1 = localizer.GetLocalized("AlertAddCardToDeckPart1");
            AlertAddCardsToDeckPart2 = localizer.GetLocalized("AlertAddCardsToDeckPart2");
            AlertAddCardsToDeckPart3 = localizer.GetLocalized("AlertAddCardsToDeckPart3");
            AlertAddCardToDeckPart3 = localizer.GetLocalized("AlertAddCardToDeckPart3");
            AlertAddOneCardToDeck = localizer.GetLocalized("AlertAddOneCardToDeck");
            CardAlreadyInDeck = localizer.GetLocalized("CardAlreadyInDeck");
            CardsAlreadyInDeck = localizer.GetLocalized("CardsAlreadyInDeck");

            CardAlreadyNotInDeck = localizer.GetLocalized("CardAlreadyNotInDeck");
            CardsAlreadyNotInDeck = localizer.GetLocalized("CardsAlreadyNotInDeck");
            AlertRemoveOneCardFromDeck = localizer.GetLocalized("AlertRemoveOneCardFromDeck");
            AlertRemoveCardFromDeckPart1 = localizer.GetLocalized("AlertRemoveCardFromDeckPart1");
            AlertRemoveCardsFromDeckPart1 = localizer.GetLocalized("AlertRemoveCardsFromDeckPart1");
            AlertRemoveCardsFromDeckPart2 = localizer.GetLocalized("AlertRemoveCardsFromDeckPart2");
            AlertRemoveCardsFromDeckPart3 = localizer.GetLocalized("AlertRemoveCardsFromDeckPart3");
            AlertRemoveCardFromDeckPart3 = localizer.GetLocalized("AlertRemoveCardFromDeckPart3");

            CardAlreadyInTargetHeap = localizer.GetLocalized("CardAlreadyInTargetHeap");
            CardsAlreadyInTargetHeap = localizer.GetLocalized("CardsAlreadyInTargetHeap");
            AlertMoveOneCardToHeap = localizer.GetLocalized("AlertMoveOneCardToHeap");
            AlertMoveCardsToHeapPart1 = localizer.GetLocalized("AlertMoveCardsToHeapPart1");
            AlertMoveCardsToHeapPart2 = localizer.GetLocalized("AlertMoveCardsToHeapPart2");
            AlertMoveCardToHeapPart3 = localizer.GetLocalized("AlertMoveCardToHeapPart3");
            AlertMoveCardsToHeapPart3 = localizer.GetLocalized("AlertMoveCardsToHeapPart3");

            AlertDeleteCardsPart1 = localizer.GetLocalized("AlertDeleteCardsPart1");
            AlertDeleteCardsPart2Single = localizer.GetLocalized("AlertDeleteCardsPart2Single");
            AlertDeleteCardsPart2Plural = localizer.GetLocalized("AlertDeleteCardsPart2Plural");

            SelectedRatingAndAbove = localizer.GetLocalized("SelectedRatingAndAbove");
            SelectedRatingAndBelow = localizer.GetLocalized("SelectedRatingAndBelow");
            NoRating = localizer.GetLocalized("NoRating");

            CardsRegisteredForNotif = localizer.GetLocalized("CardsRegisteredForNotif");
            CardsNotRegisteredForNotif = localizer.GetLocalized("CardsNotRegisteredForNotif");

            ConfirmSubscription = localizer.GetLocalized("ConfirmSubscription");
            Visibility = localizer.GetLocalized("Visibility");

            NotInYourDeck = localizer.GetLocalized("NotInYourDeck");
            InNoneOfYourDecks = localizer.GetLocalized("InNoneOfYourDecks");
            InYourDeck = localizer.GetLocalized("InYourDeck");
            InYourDecks = localizer.GetLocalized("InYourDecks");
            Deck = localizer.GetLocalized("Deck");
            Heap = localizer.GetLocalized("Heap");
            NbTimesInNotLearnedHeap = localizer.GetLocalized("NbTimesInNotLearnedHeap");
            BiggestHeapReached = localizer.GetLocalized("BiggestHeapReached");
            AddedToDeckOn = localizer.GetLocalized("AddedToDeckOn");
            LastLearnedOn = localizer.GetLocalized("LastLearnedOn");
            Never = localizer.GetLocalized("Never");
            ExpiredOn = localizer.GetLocalized("ExpiredOn");
            WillExpireOn = localizer.GetLocalized("WillExpireOn");
        }
        public string Any { get; }
        public string Ignore { get; }
        public string Before { get; }
        public string ThatDay { get; }
        public string After { get; }
        public string CardVisibleToMoreThanOwner { get; }
        public string CardVisibleToOwnerOnly { get; }
        public string IncludeCards { get; }
        public string ExcludeCards { get; }
        public string OperationIsForSelectedCards { get; }
        public string TooManySelectedCards { get; }
        public string AlertAddTagToCardsPart1 { get; }
        public string AlertAddTagToCardsPart2 { get; }
        public string AlertAddTagToCardsPart3Single { get; }
        public string AlertAddTagToCardsPart3Plural { get; }
        public string CantAddToDeckBecauseSomeCardsAlreadyIn { get; }
        public string AlertAddCardsToDeckPart1 { get; }
        public string AlertAddCardToDeckPart1 { get; }
        public string AlertAddCardsToDeckPart2 { get; }
        public string AlertAddCardsToDeckPart3 { get; }
        public string AlertAddCardToDeckPart3 { get; }
        public string AlertAddOneCardToDeck { get; }
        public string CardAlreadyInDeck { get; }
        public string CardsAlreadyInDeck { get; }
        public string CardAlreadyNotInDeck { get; }
        public string CardsAlreadyNotInDeck { get; }
        public string AlertRemoveOneCardFromDeck { get; }
        public string AlertRemoveCardFromDeckPart1 { get; }
        public string AlertRemoveCardsFromDeckPart1 { get; }
        public string AlertRemoveCardsFromDeckPart2 { get; }
        public string AlertRemoveCardsFromDeckPart3 { get; }
        public string AlertRemoveCardFromDeckPart3 { get; }
        public string CardAlreadyInTargetHeap { get; }
        public string CardsAlreadyInTargetHeap { get; }
        public string AlertMoveOneCardToHeap { get; }
        public string AlertMoveCardsToHeapPart1 { get; }
        public string AlertMoveCardsToHeapPart2 { get; }
        public string AlertMoveCardToHeapPart3 { get; }
        public string AlertMoveCardsToHeapPart3 { get; }
        public string AlertDeleteCardsPart1 { get; }
        public string AlertDeleteCardsPart2Single { get; }
        public string AlertDeleteCardsPart2Plural { get; }
        public string SelectedRatingAndAbove { get; }
        public string SelectedRatingAndBelow { get; }
        public string NoRating { get; }
        public string CardsRegisteredForNotif { get; }
        public string CardsNotRegisteredForNotif { get; }
        public string ConfirmSubscription { get; }
        public string Visibility { get; }
        public string NotInYourDeck { get; }
        public string InNoneOfYourDecks { get; }
        public string InYourDeck { get; }
        public string InYourDecks { get; }
        public string Deck { get; }
        public string Heap { get; }
        public string NbTimesInNotLearnedHeap { get; }
        public string BiggestHeapReached { get; }
        public string AddedToDeckOn { get; }
        public string LastLearnedOn { get; }
        public string Never { get; }
        public string ExpiredOn { get; }
        public string WillExpireOn { get; }
    }
    #endregion
    #endregion
    #region RunQuery
    private void CheckRunQueryRequestValidity(RunQueryRequest request)
    {
        CheckBodyParameter(request);
        if (request.RequiredTags.Contains(noTagFakeGuid))
            throw new ArgumentException("The noTagFakeGuid is not meant to be received in a request, it is just a tool for the javascript code, meaning 'remove all tags'");
        if (request.RequiredTags.Contains(allTagsFakeGuid))
            throw new ArgumentException("The allTagsFakeGuid is not meant to be received in required tags, it is meant to be used in excluded tags only");
        if (request.ExcludedTags.Contains(noTagFakeGuid))
            throw new ArgumentException("The noTagFakeGuid is not meant to be received in a request, it is just a tool for the javascript code, meaning 'remove all tags'");
        if (request.ExcludedTags.Contains(allTagsFakeGuid) && (request.RequiredTags.Count() > 1))
            throw new ArgumentException("The allTagsFakeGuid must be alone in the excluded list");
    }
    private static SearchCards.Request.VibilityFiltering AppVisibility(RunQueryRequest request)
    {
        return request.Visibility switch
        {
            1 => SearchCards.Request.VibilityFiltering.Ignore,
            2 => SearchCards.Request.VibilityFiltering.CardsVisibleByMoreThanOwner,
            3 => SearchCards.Request.VibilityFiltering.PrivateToOwner,
            _ => throw new RequestInputException($"Invalid Visibility {request.Visibility}"),
        };
    }
    private static SearchCards.Request.ReferenceFiltering AppReference(RunQueryRequest request)
    {
        return request.ReferenceFiltering switch
        {
            1 => SearchCards.Request.ReferenceFiltering.Ignore,
            2 => SearchCards.Request.ReferenceFiltering.None,
            3 => SearchCards.Request.ReferenceFiltering.NotEmpty,
            _ => throw new RequestInputException($"Invalid Reference filtering {request.Visibility}"),
        };
    }
    private static SearchCards.Request.RatingFilteringMode AppRatingMode(RunQueryRequest request)
    {
        return request.RatingFilteringMode switch
        {
            1 => SearchCards.Request.RatingFilteringMode.Ignore,
            2 => SearchCards.Request.RatingFilteringMode.AtLeast,
            3 => SearchCards.Request.RatingFilteringMode.AtMost,
            4 => SearchCards.Request.RatingFilteringMode.NoRating,
            _ => throw new RequestInputException($"Invalid RatingFilteringMode {request.RatingFilteringMode}"),
        };
    }
    private static SearchCards.Request.NotificationFiltering AppNotificationFiltering(RunQueryRequest request)
    {
        return request.NotificationFiltering switch
        {
            1 => SearchCards.Request.NotificationFiltering.Ignore,
            2 => SearchCards.Request.NotificationFiltering.RegisteredCards,
            3 => SearchCards.Request.NotificationFiltering.NotRegisteredCards,
            _ => throw new RequestInputException($"Invalid NotificationFiltering {request.NotificationFiltering}"),
        };
    }
    [HttpPost("RunQuery")]
    public async Task<IActionResult> RunQuery([FromBody] RunQueryRequest request)
    {
        CheckRunQueryRequestValidity(request);

        var user = await userManager.GetUserAsync(HttpContext.User);
        var userId = user == null ? Guid.Empty : user.Id;

        var excludedTags = (request.ExcludedTags.Count() == 1 && request.ExcludedTags.First() == allTagsFakeGuid) ? null : request.ExcludedTags;

        var applicationRequest = new SearchCards.Request
        {
            UserId = userId,
            Deck = request.Deck,
            DeckIsInclusive = request.DeckIsInclusive,
            PageNo = request.PageNo,
            PageSize = request.PageSize,
            RequiredText = request.RequiredText.Trim(),
            RequiredTags = request.RequiredTags,
            ExcludedTags = excludedTags,
            Visibility = AppVisibility(request),
            RatingFiltering = AppRatingMode(request),
            Notification = AppNotificationFiltering(request),
            Reference = AppReference(request)
        };

        if (applicationRequest.RatingFiltering != SearchCards.Request.RatingFilteringMode.Ignore)
            applicationRequest = applicationRequest with { RatingFilteringValue = request.RatingFilteringValue };
        if (request.Heap != -1)
            applicationRequest = applicationRequest with { Heap = request.Heap };

        var applicationResult = await new SearchCards(callContext).RunAsync(applicationRequest);

        var result = new RunQueryViewModel(applicationResult, this);

        return base.Ok(result);
    }
    #region Request and view model classes
    public sealed class RunQueryRequest
    {
        public int PageNo { get; set; }
        public int PageSize { get; set; }
        public Guid Deck { get; set; }
        public bool DeckIsInclusive { get; set; }   //Makes sense only if Deck is not Guid.Empty
        public int Heap { get; set; }
        public string RequiredText { get; set; } = null!;
        public int Visibility { get; set; } //1 = ignore this criteria, 2 = cards which can be seen by more than their owner, 3 = cards visible to their owner only
        public int RatingFilteringMode { get; set; } //1 = ignore this criteria, 2 = at least RatingFilteringValue, 3 = at most RatingFilteringValue, 4 = without any rating
        public int RatingFilteringValue { get; set; } //1 to 5
        public IEnumerable<Guid> RequiredTags { get; set; } = null!;
        public IEnumerable<Guid> ExcludedTags { get; set; } = null!;
        public int NotificationFiltering { get; set; } //1 = ignore this criteria, 2 = cards registered for notification, 3 = cards not registered for notification
        public int ReferenceFiltering { get; set; } //1 = ignore, 2 = none, 3 = not empty
    }
    public sealed class RunQueryViewModel
    {
        public RunQueryViewModel(SearchCards.Result applicationResult, ILocalized localizer)
        {
            TotalNbCards = applicationResult.TotalNbCards;
            PageCount = applicationResult.PageCount;
            Cards = applicationResult.Cards.Select(card => new RunQueryCardViewModel(card, localizer));
        }
        public int TotalNbCards { get; }
        public int PageCount { get; }
        public IEnumerable<RunQueryCardViewModel> Cards { get; }
    }
    public sealed class RunQueryCardViewModel
    {
        public RunQueryCardViewModel(SearchCards.ResultCard card, ILocalized localizer)
        {
            CardId = card.CardId;
            FrontSide = card.FrontSide;
            Tags = card.Tags.OrderBy(tag => tag);
            VisibleToCount = card.VisibleTo.Length;
            AverageRating = card.AverageRating;
            CurrentUserRating = card.CurrentUserRating;
            CountOfUserRatings = card.CountOfUserRatings;
            PopoverVisibility = VisibleToCount switch
            {
                0 => localizer.GetLocalized("AllUsers"),
                1 => localizer.GetLocalized("YouOnly"),
                _ => string.Join(',', card.VisibleTo.Select(userWithViewOnCard => userWithViewOnCard.User.UserName)),
            };

            Decks = card.DeckInfo.Select(deckInfo =>
                new RunQueryCardDeckViewModel(
                    deckInfo.DeckId,
                    deckInfo.DeckName,
                    deckInfo.CurrentHeap,
                    DisplayServices.HeapName(deckInfo.CurrentHeap, localizer),
                    deckInfo.NbTimesInNotLearnedHeap,
                    DisplayServices.HeapName(deckInfo.BiggestHeapReached, localizer),
                    deckInfo.AddToDeckUtcTime,
                    deckInfo.LastLearnUtcTime,
                    deckInfo.Expired,
                    deckInfo.ExpiryUtcDate));
        }
        public Guid CardId { get; }
        public string FrontSide { get; }
        public IEnumerable<string> Tags { get; }
        public int VisibleToCount { get; }
        public string PopoverVisibility { get; }
        public int CurrentUserRating { get; }
        public double AverageRating { get; }
        public int CountOfUserRatings { get; }
        public IEnumerable<RunQueryCardDeckViewModel> Decks { get; }
    }
    public sealed class RunQueryCardDeckViewModel
    {
        public RunQueryCardDeckViewModel(Guid deckId, string deckName, int heapId, string heapName, int nbTimesInNotLearnedHeap, string biggestHeapReached, DateTime addToDeckUtcTime, DateTime lastLearnUtcTime, bool expired, DateTime expiryUtcDate)
        {
            DeckId = deckId;
            DeckName = deckName;
            HeapId = heapId;
            HeapName = heapName;
            NbTimesInNotLearnedHeap = nbTimesInNotLearnedHeap;
            BiggestHeapReached = biggestHeapReached;
            AddToDeckUtcTime = addToDeckUtcTime;
            LastLearnUtcTime = lastLearnUtcTime;
            Expired = expired;
            ExpiryUtcDate = expiryUtcDate;
        }
        public Guid DeckId { get; set; }
        public string DeckName { get; }
        public int HeapId { get; }
        public string HeapName { get; }
        public int NbTimesInNotLearnedHeap { get; }
        public string BiggestHeapReached { get; }
        public DateTime AddToDeckUtcTime { get; }
        public DateTime LastLearnUtcTime { get; }
        public bool Expired { get; }
        public DateTime ExpiryUtcDate { get; }
    }
    #endregion
    #endregion
    #region AddTagToCards
    [HttpPost("AddTagToCards/{tagId}"), Authorize]
    public async Task<IActionResult> AddTagToCards(Guid tagId, [FromBody] AddTagToCardsRequest request)
    {
        CheckBodyParameter(request);
        var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
        var appRequest = new AddTagToCards.Request(userId, tagId, request.CardIds);
        await new AddTagToCards(callContext).RunAsync(appRequest);
        return ControllerResultWithToast.Success(GetLocalized("TagAdded"), this);
    }
    public sealed class AddTagToCardsRequest
    {
        public IEnumerable<Guid> CardIds { get; set; } = null!;
    }
    #endregion
    #region AddCardsToDeck
    [HttpPost("AddCardsToDeck/{deckId}"), Authorize]
    public async Task<IActionResult> AddCardsToDeck(Guid deckId, [FromBody] AddCardsToDeckRequest request)
    {
        CheckBodyParameter(request);
        var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
        await new AddCardsInDeck(callContext).RunAsync(new AddCardsInDeck.Request(userId, deckId, request.CardIds.ToArray()));
        return ControllerResultWithToast.Success(request.CardIds.Count() == 1 ? GetLocalized("CardAdded") : GetLocalized("CardsAdded"), this);
    }
    public sealed class AddCardsToDeckRequest
    {
        public IEnumerable<Guid> CardIds { get; set; } = null!;
    }
    #endregion
    #region RemoveCardsFromDeck
    [HttpPost("RemoveCardsFromDeck/{deckId}"), Authorize]
    public async Task<IActionResult> RemoveCardsFromDeckAsync(Guid deckId, [FromBody] RemoveCardsFromDeckRequest request)
    {
        CheckBodyParameter(request);
        var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
        await new RemoveCardsFromDeck(callContext).RunAsync(new RemoveCardsFromDeck.Request(userId, deckId, request.CardIds));
        return ControllerResultWithToast.Success(request.CardIds.Count() == 1 ? GetLocalized("CardRemoved") : GetLocalized("CardsRemoved"), this);
    }
    public sealed class RemoveCardsFromDeckRequest
    {
        public IEnumerable<Guid> CardIds { get; set; } = null!;
    }
    #endregion
    #region MoveCardsToHeap
    [HttpPost("MoveCardsToHeap/{deckId}/{heapId}"), Authorize]
    public async Task<IActionResult> MoveCardsToHeap(Guid deckId, int heapId, [FromBody] MoveCardsToHeapRequest request)
    {
        CheckBodyParameter(request);
        var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
        var appRequest = new MoveCardsToHeap.Request(userId, deckId, heapId, request.CardIds);
        await new MoveCardsToHeap(callContext).RunAsync(appRequest);
        return ControllerResultWithToast.Success(request.CardIds.Count() == 1 ? GetLocalized("CardMoved") : GetLocalized("CardsMoved"), this);

    }
    public sealed class MoveCardsToHeapRequest
    {
        public IEnumerable<Guid> CardIds { get; set; } = null!;
    }
    #endregion
    #region DeleteCards
    [HttpPost("DeleteCards"), Authorize]
    public async Task<IActionResult> DeleteCards([FromBody] DeleteCardsRequest request)
    {
        CheckBodyParameter(request);
        var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
        var appRequest = new DeleteCards.Request(userId, request.CardIds);
        await new DeleteCards(callContext).RunAsync(appRequest);
        return ControllerResultWithToast.Success(request.CardIds.Count() == 1 ? GetLocalized("CardDeleted") : GetLocalized("CardsDeleted"), this);

    }
    public sealed class DeleteCardsRequest
    {
        public IEnumerable<Guid> CardIds { get; set; } = null!;
    }
    #endregion
    #region RegisterForNotifications
    [HttpPost("RegisterForNotifications"), Authorize]
    public async Task<IActionResult> RegisterForNotifications([FromBody] RegisterForNotificationsRequest request)
    {
        CheckBodyParameter(request);
        var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
        var appRequest = new AddCardSubscriptions.Request(userId, request.CardIds);
        await new AddCardSubscriptions(callContext).RunAsync(appRequest);
        return ControllerResultWithToast.Success(GetLocalized("Registered"), this);
    }
    public sealed class RegisterForNotificationsRequest
    {
        public IEnumerable<Guid> CardIds { get; set; } = null!;
    }
    #endregion
    #region UnregisterForNotifications
    [HttpPost("UnregisterForNotifications"), Authorize]
    public async Task<IActionResult> UnregisterForNotifications([FromBody] UnregisterForNotificationsRequest request)
    {
        CheckBodyParameter(request);
        var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
        var appRequest = new RemoveCardSubscriptions.Request(userId, request.CardIds);
        await new RemoveCardSubscriptions(callContext).RunAsync(appRequest);
        return ControllerResultWithToast.Success(GetLocalized("Unregistered"), this);
    }
    public sealed class UnregisterForNotificationsRequest
    {
        public IEnumerable<Guid> CardIds { get; set; } = null!;
    }
    #endregion
    #region Subscribe
    private void ChecSubscribeToSearchRequestValidity(RunQueryRequest request)
    {
        CheckBodyParameter(request);
        if (request.RequiredTags.Contains(noTagFakeGuid))
            throw new ArgumentException("The noTagFakeGuid is not meant to be received in a request, it is just a tool for the javascript code, meaning 'remove all tags'");
        if (request.RequiredTags.Contains(allTagsFakeGuid))
            throw new ArgumentException("The allTagsFakeGuid is not meant to be received in required tags, it is meant to be used in excluded tags only");
        if (request.ExcludedTags.Contains(noTagFakeGuid))
            throw new ArgumentException("The noTagFakeGuid is not meant to be received in a request, it is just a tool for the javascript code, meaning 'remove all tags'");
        if (request.ExcludedTags.Contains(allTagsFakeGuid) && (request.RequiredTags.Count() > 1))
            throw new ArgumentException("The allTagsFakeGuid must be alone in the excluded list");
        if (request.Deck != Guid.Empty && request.DeckIsInclusive)
            throw new RequestInputException(GetLocalized("CanNotSubscribeToSearchInDeck"));
        if (request.Visibility != 1)
            throw new RequestInputException(GetLocalized("CanNotSubscribeToSearchWithVisibilityCriteria"));
        if (request.RatingFilteringMode != 1)
            throw new RequestInputException(GetLocalized("CanNotSubscribeToSearchWithRatingCriteria"));
    }
    [HttpPost("SubscribeToSearch")]
    public async Task<IActionResult> SubscribeToSearch([FromBody] RunQueryRequest request)
    {
        ChecSubscribeToSearchRequestValidity(request);
        var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
        var excludedTags = (request.ExcludedTags.Count() == 1 && request.ExcludedTags.First() == allTagsFakeGuid) ? null : request.ExcludedTags;
        var applicationRequest = new SubscribeToSearch.Request(userId, request.Deck, GetLocalized("NoName"), request.RequiredText, request.RequiredTags, excludedTags);
        await new SubscribeToSearch(callContext).RunAsync(applicationRequest);
        return ControllerResultWithToast.Success(GetLocalized("SubscriptionRecorded"), this);
    }
    public sealed class SubscribeToSearchViewModel
    {
        public SubscribeToSearchViewModel(string toastTitle, string toastMesg)
        {
            ToastTitle = toastTitle;
            ToastMesg = toastMesg;
        }
        public string ToastTitle { get; }
        public string ToastMesg { get; }
    }
    #endregion
    #region GetAllCardsListData
    [HttpGet("GetAllCardsListData")]
    public async Task<IActionResult> GetAllCardsListData()
    {
        var publicCards = await new GetAllPublicCards(callContext).RunAsync(new GetAllPublicCards.Request());
        var resultCards = publicCards.Cards.Select(publicCard => new GetAllCardsListDataResultEntry(publicCard.CardId, publicCard.FrontSide, publicCard.BackSide, publicCard.VersionUtcDate, publicCard.AverageRating));

        var nonPublicCardCount = await new CountNonPublicCards(callContext).RunAsync(new CountNonPublicCards.Request());

        var user = await userManager.GetUserAsync(HttpContext.User);
        var result = new GetAllCardsListDataResult(resultCards, nonPublicCardCount.Count, user);
        await Task.Delay(0);
        return base.Ok(result);
    }
    #region Request and view model classes
    public sealed class GetAllCardsListDataResult
    {
        public GetAllCardsListDataResult(IEnumerable<GetAllCardsListDataResultEntry> publicCards, int nonPublicCardCount, MemCheckUser? currentUser)
        {
            PublicCards = publicCards;
            NonPublicCardCount = nonPublicCardCount;
            ShowDebugInfo = DisplayServices.ShowDebugInfo(currentUser);
        }
        public int NonPublicCardCount { get; }
        public IEnumerable<GetAllCardsListDataResultEntry> PublicCards { get; }
        public bool ShowDebugInfo { get; }
    }
    public sealed class GetAllCardsListDataResultEntry
    {
        public GetAllCardsListDataResultEntry(Guid cardId, string frontSide, string backSide, DateTime versionUtcDate, double averageRating)
        {
            CardId = cardId;
            FrontSide = frontSide;
            BackSide = backSide;
            VersionUtcDate = versionUtcDate;
            AverageRating = averageRating;
        }
        public Guid CardId { get; }
        public string FrontSide { get; }
        public string BackSide { get; }
        public DateTime VersionUtcDate { get; }
        public double AverageRating { get; }
    }
    #endregion
    #endregion
}
