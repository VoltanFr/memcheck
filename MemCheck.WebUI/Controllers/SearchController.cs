using MemCheck.Application;
using MemCheck.Application.CardChanging;
using MemCheck.Application.Notifying;
using MemCheck.Application.QueryValidation;
using MemCheck.Application.Searching;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.WebUI.Controllers
{
    [Route("[controller]")]
    public class SearchController : Controller, ILocalized
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        private readonly UserManager<MemCheckUser> userManager;
        private readonly IStringLocalizer<SearchController> localizer;
        private readonly IStringLocalizer<DecksController> decksControllerLocalizer;
        private static readonly Guid noTagFakeGuid = Guid.Empty;
        private static readonly Guid allTagsFakeGuid = new Guid("11111111-1111-1111-1111-111111111111");
        #endregion
        public SearchController(MemCheckDbContext dbContext, UserManager<MemCheckUser> userManager, IStringLocalizer<SearchController> localizer, IStringLocalizer<DecksController> decksControllerLocalizer) : base()
        {
            this.dbContext = dbContext;
            this.userManager = userManager;
            this.localizer = localizer;
            this.decksControllerLocalizer = decksControllerLocalizer;
        }
        public IStringLocalizer Localizer => localizer;
        #region GetAllStaticData
        [HttpGet("GetAllStaticData")]
        public async Task<IActionResult> GetAllStaticData()
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            IEnumerable<GetUserDecksWithHeapsAndTags.ResultModel> decksWithHeapsAndTags;
            if (user == null)
                decksWithHeapsAndTags = new GetUserDecksWithHeapsAndTags.ResultModel[0];
            else
                decksWithHeapsAndTags = await new GetUserDecksWithHeapsAndTags(dbContext).RunAsync(user.Id);
            var allTags = new GetAllAvailableTags(dbContext).Run();
            var allUsers = new GetUsers(dbContext).Run();
            GetAllStaticDataViewModel value = new GetAllStaticDataViewModel(decksWithHeapsAndTags, allTags, allUsers, localizer, decksControllerLocalizer, user);
            return base.Ok(value);
        }
        #region View model classes
        public sealed class GetAllStaticDataViewModel
        {
            public GetAllStaticDataViewModel(IEnumerable<GetUserDecksWithHeapsAndTags.ResultModel> userDecks, IEnumerable<GetAllAvailableTags.ViewModel> allTags, IEnumerable<GetUsers.ViewModel> allUsers, IStringLocalizer<SearchController> localizer, IStringLocalizer<DecksController> decksControllerLocalizer, MemCheckUser? currentUser)
            {
                UserDecks = new[] { new GetAllStaticDataDeckViewModel(Guid.Empty, localizer["Ignore"].Value) }
                    .Concat(userDecks.Select(applicationResult => new GetAllStaticDataDeckViewModel(applicationResult, localizer, decksControllerLocalizer)));
                AllDecksForAddingCards = userDecks.Select(applicationResult => new GetAllStaticDataDeckForAddViewModel(applicationResult.DeckId, applicationResult.Description));
                AllApplicableTags = allTags.Select(tag => new GetAllStaticDataTagViewModel(tag.TagId, tag.Name));
                AllRequirableTags = new[] { new GetAllStaticDataTagViewModel(noTagFakeGuid, localizer["None"].Value) }
                    .Concat(allTags.Select(tag => new GetAllStaticDataTagViewModel(tag.TagId, tag.Name)));
                AllExcludableTags = new[] {
                    new GetAllStaticDataTagViewModel(noTagFakeGuid, localizer["None"].Value),
                    new GetAllStaticDataTagViewModel(allTagsFakeGuid, localizer["All"].Value)
                    }
                    .Concat(allTags.Select(tag => new GetAllStaticDataTagViewModel(tag.TagId, tag.Name)));
                AllUsers = new[] { new GetAllStaticDataUserViewModel(Guid.Empty, localizer["Any"].Value) }
                    .Concat(allUsers.Select(user => new GetAllStaticDataUserViewModel(user.UserId, user.UserName)));
                LocalizedText = new GetAllStaticDataLocalizedTextViewModel(localizer);
                PossibleHeapsForMove = Enumerable.Range(0, CardInDeck.MaxHeapValue).Select(heapId => new GetAllStaticDataHeapViewModel(heapId, DisplayServices.HeapName(heapId, decksControllerLocalizer)));
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
            public GetAllStaticDataDeckViewModel(GetUserDecksWithHeapsAndTags.ResultModel applicationResult, IStringLocalizer<SearchController> localizer, IStringLocalizer<DecksController> decksControllerLocalizer)
            {
                DeckId = applicationResult.DeckId;
                DeckName = applicationResult.Description;
                Heaps = new[] { new GetAllStaticDataHeapViewModel(-1, localizer["Any"].Value) }
                    .Concat(applicationResult.Heaps.OrderBy(heap => heap).Select(heap => new GetAllStaticDataHeapViewModel(heap, DisplayServices.HeapName(heap, decksControllerLocalizer))));
                RequirableTags = new[] { new GetAllStaticDataTagViewModel(noTagFakeGuid, localizer["None"].Value) }
                    .Concat(applicationResult.Tags.Select(tag => new GetAllStaticDataTagViewModel(tag.TagId, tag.TagName)));
                ExcludableTags = new[] {
                    new GetAllStaticDataTagViewModel(noTagFakeGuid, localizer["None"].Value),
                    new GetAllStaticDataTagViewModel(allTagsFakeGuid, localizer["All"].Value)
                    }
                    .Concat(applicationResult.Tags.Select(tag => new GetAllStaticDataTagViewModel(tag.TagId, tag.TagName)));
            }
            public GetAllStaticDataDeckViewModel(Guid deckId, string deckName)
            {
                DeckId = deckId;
                DeckName = deckName;
                Heaps = new GetAllStaticDataHeapViewModel[0];
                RequirableTags = new GetAllStaticDataTagViewModel[0];
                ExcludableTags = new GetAllStaticDataTagViewModel[0];
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
            public GetAllStaticDataLocalizedTextViewModel(IStringLocalizer<SearchController> localizer)
            {
                Any = localizer["Any"].Value;
                Ignore = localizer["Ignore"].Value;
                Before = localizer["Before"].Value;
                ThatDay = localizer["On"].Value;
                After = localizer["After"].Value;
                CardVisibleToMoreThanOwner = localizer["CardVisibleToMoreThanOwner"].Value;
                CardVisibleToOwnerOnly = localizer["CardVisibleToOwnerOnly"].Value;
                IncludeCards = localizer["IncludeCards"].Value;
                ExcludeCards = localizer["ExcludeCards"].Value;
                OperationIsForSelectedCards = localizer["OperationIsForSelectedCards"].Value;
                AlertAddTagToCardsPart1 = localizer["AlertAddTagToCardsPart1"].Value;
                AlertAddTagToCardsPart2 = localizer["AlertAddTagToCardsPart2"].Value;
                AlertAddTagToCardsPart3Single = localizer["AlertAddTagToCardsPart3Single"].Value;
                AlertAddTagToCardsPart3Plural = localizer["AlertAddTagToCardsPart3Plural"].Value;
                TagAdded = localizer["TagAdded"].Value;
                CantAddToDeckBecauseSomeCardsAlreadyIn = localizer["CantAddToDeckBecauseSomeCardsAlreadyIn"].Value;
                AlertAddCardsToDeckPart1 = localizer["AlertAddCardsToDeckPart1"].Value;
                AlertAddCardToDeckPart1 = localizer["AlertAddCardToDeckPart1"].Value;
                AlertAddCardsToDeckPart2 = localizer["AlertAddCardsToDeckPart2"].Value;
                AlertAddCardsToDeckPart3 = localizer["AlertAddCardsToDeckPart3"].Value;
                AlertAddCardToDeckPart3 = localizer["AlertAddCardToDeckPart3"].Value;
                AlertAddOneCardToDeck = localizer["AlertAddOneCardToDeck"].Value;
                CardsAdded = localizer["CardsAdded"].Value;
                CardAdded = localizer["CardAdded"].Value;
                CardAlreadyInDeck = localizer["CardAlreadyInDeck"].Value;
                CardsAlreadyInDeck = localizer["CardsAlreadyInDeck"].Value;

                CardAlreadyNotInDeck = localizer["CardAlreadyNotInDeck"].Value;
                CardsAlreadyNotInDeck = localizer["CardsAlreadyNotInDeck"].Value;
                AlertRemoveOneCardFromDeck = localizer["AlertRemoveOneCardFromDeck"].Value;
                AlertRemoveCardFromDeckPart1 = localizer["AlertRemoveCardFromDeckPart1"].Value;
                AlertRemoveCardsFromDeckPart1 = localizer["AlertRemoveCardsFromDeckPart1"].Value;
                AlertRemoveCardsFromDeckPart2 = localizer["AlertRemoveCardsFromDeckPart2"].Value;
                AlertRemoveCardsFromDeckPart3 = localizer["AlertRemoveCardsFromDeckPart3"].Value;
                AlertRemoveCardFromDeckPart3 = localizer["AlertRemoveCardFromDeckPart3"].Value;
                CardRemoved = localizer["CardRemoved"].Value;
                CardsRemoved = localizer["CardsRemoved"].Value;

                OperationIsForFilteringOnDeckInclusive = localizer["OperationIsForFilteringOnDeckInclusive"].Value;
                CardAlreadyInTargetHeap = localizer["CardAlreadyInTargetHeap"].Value;
                CardsAlreadyInTargetHeap = localizer["CardsAlreadyInTargetHeap"].Value;
                AlertMoveOneCardToHeap = localizer["AlertMoveOneCardToHeap"].Value;
                AlertMoveCardsToHeapPart1 = localizer["AlertMoveCardsToHeapPart1"].Value;
                AlertMoveCardsToHeapPart2 = localizer["AlertMoveCardsToHeapPart2"].Value;
                AlertMoveCardToHeapPart3 = localizer["AlertMoveCardToHeapPart3"].Value;
                AlertMoveCardsToHeapPart3 = localizer["AlertMoveCardsToHeapPart3"].Value;
                CardMoved = localizer["CardMoved"].Value;
                CardsMoved = localizer["CardsMoved"].Value;

                AlertDeleteCardsPart1 = localizer["AlertDeleteCardsPart1"].Value;
                AlertDeleteCardsPart2Single = localizer["AlertDeleteCardsPart2Single"].Value;
                AlertDeleteCardsPart2Plural = localizer["AlertDeleteCardsPart2Plural"].Value;
                CardDeleted = localizer["CardDeleted"].Value;
                CardsDeleted = localizer["CardsDeleted"].Value;

                SelectedRatingAndAbove = localizer["SelectedRatingAndAbove"].Value;
                SelectedRatingAndBelow = localizer["SelectedRatingAndBelow"].Value;
                NoRating = localizer["NoRating"].Value;

                CardsRegisteredForNotif = localizer["CardsRegisteredForNotif"].Value;
                CardsNotRegisteredForNotif = localizer["CardsNotRegisteredForNotif"].Value;
                Registered = localizer["Registered"].Value;
                Unregistered = localizer["Unregistered"].Value;

                ConfirmSubscription = localizer["ConfirmSubscription"].Value;
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
            public string AlertAddTagToCardsPart1 { get; }
            public string AlertAddTagToCardsPart2 { get; }
            public string AlertAddTagToCardsPart3Single { get; }
            public string AlertAddTagToCardsPart3Plural { get; }
            public string TagAdded { get; }
            public string CantAddToDeckBecauseSomeCardsAlreadyIn { get; }
            public string AlertAddCardsToDeckPart1 { get; }
            public string AlertAddCardToDeckPart1 { get; }
            public string AlertAddCardsToDeckPart2 { get; }
            public string AlertAddCardsToDeckPart3 { get; }
            public string AlertAddCardToDeckPart3 { get; }
            public string AlertAddOneCardToDeck { get; }
            public string CardsAdded { get; }
            public string CardAdded { get; }
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
            public string CardRemoved { get; }
            public string CardsRemoved { get; }
            public string OperationIsForFilteringOnDeckInclusive { get; }
            public string CardAlreadyInTargetHeap { get; }
            public string CardsAlreadyInTargetHeap { get; }
            public string AlertMoveOneCardToHeap { get; }
            public string AlertMoveCardsToHeapPart1 { get; }
            public string AlertMoveCardsToHeapPart2 { get; }
            public string AlertMoveCardToHeapPart3 { get; }
            public string AlertMoveCardsToHeapPart3 { get; }
            public string CardMoved { get; }
            public string CardsMoved { get; }
            public string AlertDeleteCardsPart1 { get; }
            public string AlertDeleteCardsPart2Single { get; }
            public string AlertDeleteCardsPart2Plural { get; }
            public string CardDeleted { get; }
            public string CardsDeleted { get; }
            public string SelectedRatingAndAbove { get; }
            public string SelectedRatingAndBelow { get; }
            public string NoRating { get; }
            public string CardsRegisteredForNotif { get; }
            public string CardsNotRegisteredForNotif { get; }
            public string Registered { get; }
            public string Unregistered { get; }
            public string ConfirmSubscription { get; }
        }
        #endregion
        #endregion
        #region RunQuery
        private void CheckRunQueryRequestValidity(RunQueryRequest request)
        {
            if (request == null)
                throw new ArgumentException("Request not received, probably a serialization problem");
            if (request.RequiredTags.Contains(noTagFakeGuid))
                throw new ArgumentException("The noTagFakeGuid is not meant to be received in a request, it is just a tool for the javascript code, meaning 'remove all tags'");
            if (request.RequiredTags.Contains(allTagsFakeGuid))
                throw new ArgumentException("The allTagsFakeGuid is not meant to be received in required tags, it is meant to be used in excluded tags only");
            if (request.ExcludedTags.Contains(noTagFakeGuid))
                throw new ArgumentException("The noTagFakeGuid is not meant to be received in a request, it is just a tool for the javascript code, meaning 'remove all tags'");
            if (request.ExcludedTags.Contains(allTagsFakeGuid) && (request.RequiredTags.Count() > 1))
                throw new ArgumentException("The allTagsFakeGuid must be alone in the excluded list");
        }
        private SearchCards.Request.VibilityFiltering AppVisibility(RunQueryRequest request)
        {
            switch (request.Visibility)
            {
                case 1: return SearchCards.Request.VibilityFiltering.Ignore;
                case 2: return SearchCards.Request.VibilityFiltering.CardsVisibleByMoreThanOwner;
                case 3: return SearchCards.Request.VibilityFiltering.PrivateToOwner;
                default: throw new RequestInputException($"Invalid Visibility {request.Visibility}");
            }
        }
        private SearchCards.Request.RatingFilteringMode AppRatingMode(RunQueryRequest request)
        {
            switch (request.RatingFilteringMode)
            {
                case 1: return SearchCards.Request.RatingFilteringMode.Ignore;
                case 2: return SearchCards.Request.RatingFilteringMode.AtLeast;
                case 3: return SearchCards.Request.RatingFilteringMode.AtMost;
                case 4: return SearchCards.Request.RatingFilteringMode.NoRating;
                default: throw new RequestInputException($"Invalid RatingFilteringMode {request.RatingFilteringMode}");
            }
        }
        private SearchCards.Request.NotificationFiltering AppNotificationFiltering(RunQueryRequest request)
        {
            switch (request.NotificationFiltering)
            {
                case 1: return SearchCards.Request.NotificationFiltering.Ignore;
                case 2: return SearchCards.Request.NotificationFiltering.RegisteredCards;
                case 3: return SearchCards.Request.NotificationFiltering.NotRegisteredCards;
                default: throw new RequestInputException($"Invalid NotificationFiltering {request.NotificationFiltering}");
            }
        }
        [HttpPost("RunQuery")]
        public async Task<IActionResult> RunQuery([FromBody] RunQueryRequest request)
        {
            try
            {
                CheckRunQueryRequestValidity(request);

                var user = await userManager.GetUserAsync(HttpContext.User);
                var userId = user == null ? Guid.Empty : user.Id;
                var userName = user == null ? null : user.UserName;

                var excludedTags = (request.ExcludedTags.Count() == 1 && request.ExcludedTags.First() == allTagsFakeGuid) ? null : request.ExcludedTags;

                var applicationRequest = new SearchCards.Request
                {
                    UserId = userId,
                    Deck = request.Deck,
                    DeckIsInclusive = request.DeckIsInclusive,
                    PageNo = request.PageNo,
                    PageSize = request.PageSize,
                    RequiredText = request.RequiredText,
                    RequiredTags = request.RequiredTags,
                    ExcludedTags = excludedTags,
                    Visibility = AppVisibility(request),
                    RatingFiltering = AppRatingMode(request),
                    Notification = AppNotificationFiltering(request)
                };

                if (applicationRequest.RatingFiltering != SearchCards.Request.RatingFilteringMode.Ignore)
                    applicationRequest = applicationRequest with { RatingFilteringValue = request.RatingFilteringValue };
                if (request.Heap != -1)
                    applicationRequest = applicationRequest with { Heap = request.Heap };

                var applicationResult = await new SearchCards(dbContext).RunAsync(applicationRequest);

                var result = new RunQueryViewModel(applicationResult, userName, localizer, decksControllerLocalizer);

                return base.Ok(result);
            }
            catch (SearchResultTooBigForRatingException)
            {
                return ControllerError.BadRequest(localizer["SearchTooBigForRatingFiltering"].Value, this);
            }
            catch (Exception e)
            {
                return ControllerError.BadRequest(e, this);
            }
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
        }
        public sealed class RunQueryViewModel
        {
            public RunQueryViewModel(SearchCards.Result applicationResult, string? currentUser, IStringLocalizer<SearchController> localizer, IStringLocalizer<DecksController> decksControllerLocalizer)
            {
                TotalNbCards = applicationResult.TotalNbCards;
                PageCount = applicationResult.PageCount;
                Cards = applicationResult.Cards.Select(card => new RunQueryCardViewModel(card, currentUser, localizer, decksControllerLocalizer));
            }
            public int TotalNbCards { get; }
            public int PageCount { get; }
            public IEnumerable<RunQueryCardViewModel> Cards { get; }
        }
        public sealed class RunQueryCardViewModel
        {
            public RunQueryCardViewModel(SearchCards.ResultCard card, string? currentUser, IStringLocalizer<SearchController> localizer, IStringLocalizer<DecksController> decksControllerLocalizer)
            {
                CardId = card.CardId;
                FrontSide = card.FrontSide;
                Tags = card.Tags.OrderBy(tag => tag);
                VisibleToCount = card.VisibleTo.Count();
                AverageRating = card.AverageRating;
                CurrentUserRating = card.CurrentUserRating;
                CountOfUserRatings = card.CountOfUserRatings;
                PopoverVisibility = localizer["Visibility"].Value;
                if (VisibleToCount == 1)
                    PopoverVisibility = localizer["YouOnly"].ToString();
                else
                {
                    if (VisibleToCount == 0)
                        PopoverVisibility = localizer["AllUsers"].ToString();
                    else
                        PopoverVisibility = string.Join(',', card.VisibleTo);
                }

                Decks = card.DeckInfo.Select(deckInfo =>
                    new RunQueryCardDeckViewModel(
                        deckInfo.DeckId,
                        deckInfo.DeckName,
                        deckInfo.CurrentHeap,
                        DisplayServices.HeapName(deckInfo.CurrentHeap, decksControllerLocalizer),
                        deckInfo.NbTimesInNotLearnedHeap,
                        DisplayServices.HeapName(deckInfo.BiggestHeapReached, decksControllerLocalizer),
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
            var user = await userManager.GetUserAsync(HttpContext.User);
            var appRequest = new AddTagToCards.Request(user, tagId, request.CardIds);
            await new AddTagToCards(dbContext, localizer).RunAsync(appRequest);

            return Ok();
        }
        public sealed class AddTagToCardsRequest
        {
            public IEnumerable<Guid> CardIds { get; set; } = null!;
        }
        #endregion
        #region AddCardsToDeck
        [HttpPost("AddCardsToDeck/{deckId}"), Authorize]
        public IActionResult AddCardsToDeck(Guid deckId, [FromBody] AddCardsToDeckRequest request)
        {
            new AddCardsInDeck(dbContext).Run(deckId, request.CardIds);
            return Ok();
        }
        public sealed class AddCardsToDeckRequest
        {
            public IEnumerable<Guid> CardIds { get; set; } = null!;
        }
        #endregion
        #region RemoveCardsFromDeck
        [HttpPost("RemoveCardsFromDeck/{deckId}"), Authorize]
        public IActionResult RemoveCardsFromDeck(Guid deckId, [FromBody] RemoveCardsFromDeckRequest request)
        {
            new RemoveCardsFromDeck(dbContext).Run(deckId, request.CardIds);
            return Ok();
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
            var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
            var appRequest = new MoveCardsToHeap.Request(userId, deckId, heapId, request.CardIds);
            await new MoveCardsToHeap(dbContext).RunAsync(appRequest);
            return Ok();
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
            try
            {
                var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
                var appRequest = new DeleteCards.Request(userId, request.CardIds);
                await new DeleteCards(dbContext, localizer).RunAsync(appRequest);
                return Ok();
            }
            catch (Exception e)
            {
                return ControllerError.BadRequest(e, this);
            }
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
            try
            {
                var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
                var appRequest = new AddCardSubscriptions.Request(userId, request.CardIds);
                await new AddCardSubscriptions(dbContext).RunAsync(appRequest);
                return Ok();
            }
            catch (Exception e)
            {
                return ControllerError.BadRequest(e, this);
            }
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
            try
            {
                var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
                var appRequest = new RemoveCardSubscriptions.Request(userId, request.CardIds);
                await new RemoveCardSubscriptions(dbContext).RunAsync(appRequest);
                return Ok();
            }
            catch (Exception e)
            {
                return ControllerError.BadRequest(e, this);
            }
        }
        public sealed class UnregisterForNotificationsRequest
        {
            public IEnumerable<Guid> CardIds { get; set; } = null!;
        }
        #endregion
        #region Subscribe
        private void ChecSubscribeToSearchRequestValidity(RunQueryRequest request)
        {
            if (request == null)
                throw new ArgumentException("Request not received, probably a serialization problem");
            if (request.RequiredTags.Contains(noTagFakeGuid))
                throw new ArgumentException("The noTagFakeGuid is not meant to be received in a request, it is just a tool for the javascript code, meaning 'remove all tags'");
            if (request.RequiredTags.Contains(allTagsFakeGuid))
                throw new ArgumentException("The allTagsFakeGuid is not meant to be received in required tags, it is meant to be used in excluded tags only");
            if (request.ExcludedTags.Contains(noTagFakeGuid))
                throw new ArgumentException("The noTagFakeGuid is not meant to be received in a request, it is just a tool for the javascript code, meaning 'remove all tags'");
            if (request.ExcludedTags.Contains(allTagsFakeGuid) && (request.RequiredTags.Count() > 1))
                throw new ArgumentException("The allTagsFakeGuid must be alone in the excluded list");
            if (request.Deck != Guid.Empty && request.DeckIsInclusive)
                throw new RequestInputException(localizer["CanNotSubscribeToSearchInDeck"].Value);
            if (request.Visibility != 1)
                throw new RequestInputException(localizer["CanNotSubscribeToSearchWithVisibilityCriteria"].Value);
            if (request.RatingFilteringMode != 1)
                throw new RequestInputException(localizer["CanNotSubscribeToSearchWithRatingCriteria"].Value);
        }
        [HttpPost("SubscribeToSearch")]
        public async Task<IActionResult> SubscribeToSearch([FromBody] RunQueryRequest request)
        {
            try
            {
                ChecSubscribeToSearchRequestValidity(request);
                var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
                var excludedTags = (request.ExcludedTags.Count() == 1 && request.ExcludedTags.First() == allTagsFakeGuid) ? null : request.ExcludedTags;
                var applicationRequest = new SubscribeToSearch.Request(userId, request.Deck, request.RequiredText, request.RequiredTags, excludedTags);
                await new SubscribeToSearch(dbContext).RunAsync(applicationRequest);
                var result = new SubscribeToSearchViewModel(localizer["Success"].Value, localizer["SubscriptionRecorded"].Value);
                return base.Ok(result);
            }
            catch (Exception e)
            {
                return ControllerError.BadRequest(e, this);
            }
        }
        public sealed class SubscribeToSearchViewModel
        {
            public SubscribeToSearchViewModel(string toastTitle, string toastMesg)
            {
                this.toastTitle = toastTitle;
                ToastMesg = toastMesg;
            }
            public string toastTitle { get; }
            public string ToastMesg { get; }
        }
        #endregion
    }
}
