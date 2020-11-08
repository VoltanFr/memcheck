using MemCheck.Application;
using MemCheck.Database;
using MemCheck.Domain;
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
                UserDecks = new[] { new GetAllStaticDataDeckViewModel(Guid.Empty, localizer["Ignore"]) }
                    .Concat(userDecks.Select(applicationResult => new GetAllStaticDataDeckViewModel(applicationResult, localizer, decksControllerLocalizer)));
                AllDecksForAddingCards = userDecks.Select(applicationResult => new GetAllStaticDataDeckForAddViewModel(applicationResult.DeckId, applicationResult.Description));
                AllApplicableTags = allTags.Select(tag => new GetAllStaticDataTagViewModel(tag.TagId, tag.Name));
                AllRequirableTags = new[] { new GetAllStaticDataTagViewModel(noTagFakeGuid, localizer["None"]) }
                    .Concat(allTags.Select(tag => new GetAllStaticDataTagViewModel(tag.TagId, tag.Name)));
                AllExcludableTags = new[] {
                    new GetAllStaticDataTagViewModel(noTagFakeGuid, localizer["None"]),
                    new GetAllStaticDataTagViewModel(allTagsFakeGuid, localizer["All"])
                    }
                    .Concat(allTags.Select(tag => new GetAllStaticDataTagViewModel(tag.TagId, tag.Name)));
                AllUsers = new[] { new GetAllStaticDataUserViewModel(Guid.Empty, localizer["Any"]) }
                    .Concat(allUsers.Select(user => new GetAllStaticDataUserViewModel(user.UserId, user.UserName)));
                LocalizedText = new GetAllStaticDataLocalizedTextViewModel(localizer);
                PossibleHeapsForMove = Enumerable.Range(0, MoveCardToHeap.MaxTargetHeapId).Select(heapId => new GetAllStaticDataHeapViewModel(heapId, DisplayServices.HeapName(heapId, decksControllerLocalizer)));
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
                Heaps = new[] { new GetAllStaticDataHeapViewModel(-1, localizer["Any"]) }
                    .Concat(applicationResult.Heaps.OrderBy(heap => heap).Select(heap => new GetAllStaticDataHeapViewModel(heap, DisplayServices.HeapName(heap, decksControllerLocalizer))));
                RequirableTags = new[] { new GetAllStaticDataTagViewModel(noTagFakeGuid, localizer["None"]) }
                    .Concat(applicationResult.Tags.Select(tag => new GetAllStaticDataTagViewModel(tag.TagId, tag.TagName)));
                ExcludableTags = new[] {
                    new GetAllStaticDataTagViewModel(noTagFakeGuid, localizer["None"]),
                    new GetAllStaticDataTagViewModel(allTagsFakeGuid, localizer["All"])
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
                Any = localizer["Any"];
                Ignore = localizer["Ignore"];
                Before = localizer["Before"];
                ThatDay = localizer["On"];
                After = localizer["After"];
                CardVisibleToMoreThanOwner = localizer["CardVisibleToMoreThanOwner"];
                CardVisibleToOwnerOnly = localizer["CardVisibleToOwnerOnly"];
                IncludeCards = localizer["IncludeCards"];
                ExcludeCards = localizer["ExcludeCards"];
                OperationIsForSelectedCards = localizer["OperationIsForSelectedCards"];
                AlertAddTagToCardsPart1 = localizer["AlertAddTagToCardsPart1"];
                AlertAddTagToCardsPart2 = localizer["AlertAddTagToCardsPart2"];
                AlertAddTagToCardsPart3Single = localizer["AlertAddTagToCardsPart3Single"];
                AlertAddTagToCardsPart3Plural = localizer["AlertAddTagToCardsPart3Plural"];
                TagAdded = localizer["TagAdded"];
                CantAddToDeckBecauseSomeCardsAlreadyIn = localizer["CantAddToDeckBecauseSomeCardsAlreadyIn"];
                AlertAddCardsToDeckPart1 = localizer["AlertAddCardsToDeckPart1"];
                AlertAddCardToDeckPart1 = localizer["AlertAddCardToDeckPart1"];
                AlertAddCardsToDeckPart2 = localizer["AlertAddCardsToDeckPart2"];
                AlertAddCardsToDeckPart3 = localizer["AlertAddCardsToDeckPart3"];
                AlertAddCardToDeckPart3 = localizer["AlertAddCardToDeckPart3"];
                AlertAddOneCardToDeck = localizer["AlertAddOneCardToDeck"];
                CardsAdded = localizer["CardsAdded"];
                CardAdded = localizer["CardAdded"];
                CardAlreadyInDeck = localizer["CardAlreadyInDeck"];
                CardsAlreadyInDeck = localizer["CardsAlreadyInDeck"];

                CardAlreadyNotInDeck = localizer["CardAlreadyNotInDeck"];
                CardsAlreadyNotInDeck = localizer["CardsAlreadyNotInDeck"];
                AlertRemoveOneCardFromDeck = localizer["AlertRemoveOneCardFromDeck"];
                AlertRemoveCardFromDeckPart1 = localizer["AlertRemoveCardFromDeckPart1"];
                AlertRemoveCardsFromDeckPart1 = localizer["AlertRemoveCardsFromDeckPart1"];
                AlertRemoveCardsFromDeckPart2 = localizer["AlertRemoveCardsFromDeckPart2"];
                AlertRemoveCardsFromDeckPart3 = localizer["AlertRemoveCardsFromDeckPart3"];
                AlertRemoveCardFromDeckPart3 = localizer["AlertRemoveCardFromDeckPart3"];
                CardRemoved = localizer["CardRemoved"];
                CardsRemoved = localizer["CardsRemoved"];

                OperationIsForFilteringOnDeckInclusive = localizer["OperationIsForFilteringOnDeckInclusive"];
                CardAlreadyInTargetHeap = localizer["CardAlreadyInTargetHeap"];
                CardsAlreadyInTargetHeap = localizer["CardsAlreadyInTargetHeap"];
                AlertMoveOneCardToHeap = localizer["AlertMoveOneCardToHeap"];
                AlertMoveCardsToHeapPart1 = localizer["AlertMoveCardsToHeapPart1"];
                AlertMoveCardsToHeapPart2 = localizer["AlertMoveCardsToHeapPart2"];
                AlertMoveCardToHeapPart3 = localizer["AlertMoveCardToHeapPart3"];
                AlertMoveCardsToHeapPart3 = localizer["AlertMoveCardsToHeapPart3"];
                CardMoved = localizer["CardMoved"];
                CardsMoved = localizer["CardsMoved"];

                AlertDeleteCardsPart1 = localizer["AlertDeleteCardsPart1"];
                AlertDeleteCardsPart2Single = localizer["AlertDeleteCardsPart2Single"];
                AlertDeleteCardsPart2Plural = localizer["AlertDeleteCardsPart2Plural"];
                CardDeleted = localizer["CardDeleted"];
                CardsDeleted = localizer["CardsDeleted"];

                SelectedRatingAndAbove = localizer["SelectedRatingAndAbove"];
                SelectedRatingAndBelow = localizer["SelectedRatingAndBelow"];
                NoRating = localizer["NoRating"];
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

                var applicationRequest = new SearchCards.Request(request.Deck, request.DeckIsInclusive, request.Heap, request.PageNo, request.PageSize, request.RequiredText, request.RequiredTags, excludedTags, request.Visibility, request.RatingFilteringMode, request.RatingFilteringValue);

                var applicationResult = new SearchCards(dbContext).Run(applicationRequest, userId);

                var result = new RunQueryViewModel(applicationResult, userName, localizer, decksControllerLocalizer);

                return base.Ok(result);
            }
            catch (SearchResultTooBigForRatingException)
            {
                return ControllerError.BadRequest(localizer["SearchTooBigForRatingFiltering"], this);
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
                PopoverVisibility = localizer["Visibility"];
                if (VisibleToCount == 1)
                {
                    var visibleToUser = card.VisibleTo.First();
                    if (visibleToUser != currentUser)
                        throw new ApplicationException($"Card visible to single user should be current user, is {visibleToUser}");
                    PopoverVisibility = localizer["YouOnly"].ToString();
                }
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
        [HttpPost("AddTagToCards/{tagId}")]
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
        [HttpPost("AddCardsToDeck/{deckId}")]
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
        [HttpPost("RemoveCardsFromDeck/{deckId}")]
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
        [HttpPost("MoveCardsToHeap/{deckId}/{heapId}")]
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
        [HttpPost("DeleteCards")]
        public async Task<IActionResult> DeleteCards([FromBody] DeleteCardsRequest request)
        {
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                var appRequest = new DeleteCards.Request(user, request.CardIds);
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
    }
}
