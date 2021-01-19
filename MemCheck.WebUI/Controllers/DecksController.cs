﻿using MemCheck.Application;
using MemCheck.Application.DeckChanging;
using MemCheck.Application.Heaping;
using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.WebUI.Controllers
{
    [Route("[controller]"), Authorize]
    public class DecksController : MemCheckController
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        private readonly UserManager<MemCheckUser> userManager;
        #endregion
        #region Private methods
        private string HeapingAlgoNameFromId(int id)
        {
            return Get("HeapingAlgoNameForId" + id);
        }
        private string HeapingAlgoDescriptionFromId(int id)
        {
            return Get("HeapingAlgoDescriptionForId" + id);
        }
        #endregion
        public DecksController(MemCheckDbContext dbContext, UserManager<MemCheckUser> userManager, IStringLocalizer<DecksController> localizer) : base(localizer)
        {
            this.dbContext = dbContext;
            this.userManager = userManager;
        }
        #region GetUserDecks
        [HttpGet("GetUserDecks")]
        public async Task<IActionResult> GetUserDecks()
        {
            var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
            var decks = new GetUserDecks(dbContext).Run(userId);
            var result = decks.Select(deck => new GetUserDecksViewModel(deck.DeckId, deck.Description, deck.HeapingAlgorithmId, deck.CardCount));
            return base.Ok(result);
        }
        public sealed class GetUserDecksViewModel
        {
            public GetUserDecksViewModel(Guid deckId, string description, int heapingAlgorithmId, int cardCount)
            {
                DeckId = deckId;
                Description = description;
                CardCount = cardCount;
                HeapingAlgorithmId = heapingAlgorithmId;
            }
            public string Description { get; }
            public Guid DeckId { get; }
            public int HeapingAlgorithmId { get; }
            public int CardCount { get; }
        }
        #endregion
        [HttpGet("GetCardsNotInDeck/{id}")] public IActionResult GetCardsNotInDeck(Guid id) => Ok(new GetCardsNotInDeck(dbContext).Run(id));
        [HttpGet("GetCardsInDeck/{id}")] public IActionResult GetCardsInDeck(Guid id) => Ok(new GetCardsInDeck(dbContext).Run(id));  //To be renamed to CardsInDeck
        [HttpPost("AddCardInDeck/{deckId}/{cardId}")] public async Task<IActionResult> AddCardInDeck(Guid deckId, Guid cardId) => Ok(await new AddCardInDeck(dbContext).RunAsync(deckId, cardId));
        #region RemoveCardFromDeck
        [HttpDelete("RemoveCardFromDeck/{deckId}/{cardId}")]
        public async Task<IActionResult> RemoveCardFromDeck(Guid deckId, Guid cardId)
        {
            var currentUserId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
            var query = new RemoveCardFromDeck.Request(currentUserId, deckId, cardId);
            var applicationResult = await new RemoveCardFromDeck(dbContext).RunAsync(query);
            var frontSide = $" '{applicationResult.FrontSideText.Truncate(30)}'";
            var mesgBody = Get("CardWithFrontSideHead") + frontSide + ' ' + Get("RemovedFromDeck") + ' ' + applicationResult.DeckName;
            return ControllerResultWithToast.Success(mesgBody, this);
        }
        #endregion
        #region GetHeapingAlgorithms
        [HttpGet("GetHeapingAlgorithms")]
        public IActionResult GetHeapingAlgorithms()
        {
            var ids = HeapingAlgorithms.Instance.Ids;
            var result = ids.Select(id => new HeapingAlgorithmViewModel(id, HeapingAlgoNameFromId(id), HeapingAlgoDescriptionFromId(id)));
            return base.Ok(result);
        }
        public class HeapingAlgorithmViewModel
        {
            public HeapingAlgorithmViewModel(int id, string nameInCurrentLanguage, string descriptionInCurrentLanguage)
            {
                Id = id;
                NameInCurrentLanguage = nameInCurrentLanguage;
                DescriptionInCurrentLanguage = descriptionInCurrentLanguage;
            }

            public int Id { get; }
            public string NameInCurrentLanguage { get; }
            public string DescriptionInCurrentLanguage { get; }
        }
        #endregion
        #region Create
        [HttpPost("Create")]
        async public Task<IActionResult> Create([FromBody] CreateRequest request)
        {
            CheckBodyParameter(request);
            var user = await userManager.GetUserAsync(HttpContext.User);
            var appRequest = new CreateDeck.Request(user, request.Description ?? "", request.HeapingAlgorithmId);
            return Ok(await new CreateDeck(dbContext).RunAsync(appRequest));
        }
        public sealed class CreateRequest
        {
            public string? Description { get; set; }
            public int HeapingAlgorithmId { get; set; }
        }
        #endregion
        #region Update
        [HttpPost("Update")]
        async public Task<IActionResult> Update([FromBody] UpdateDeck.Request deck)
        {
            CheckBodyParameter(deck);
            return Ok(await new UpdateDeck(dbContext).RunAsync(deck));
        }
        #endregion
        #region GetCards
        /* I think this method is useless. Commented out on 22 Dec 2020
        [HttpPost("GetCards")]
        public async Task<IActionResult> GetCardsAsync([FromBody] GetCardsRequest request)
        {
            //I wish this method was a Get instead of a Post, but I did not manager to have it work (with [FromQuery] parameter)
            CheckBodyParameter(request);
            var user = await userManager.GetUserAsync(HttpContext.User);
            var requiredTags = request.RequiredTags.Select(tag => tag.TagId);
            bool requireCardsHaveNoTag = requiredTags.Count() == 1 && requiredTags.First() == Guid.Empty;
            if (requireCardsHaveNoTag)
                requiredTags =  Array.Empty<Guid>();
            var applicationRequest = new SearchDeckCards.SearchRequest(
                request.DeckId,
                requiredTags,
                requireCardsHaveNoTag,
                request.HeapFilter == -1 ? null : (int?)request.HeapFilter,
                request.TextFilter,
                request.pageNo,
                request.pageSize);
            var applicationResult = new SearchDeckCards(dbContext).Run(applicationRequest);
            return base.Ok(new GetCardsViewModel(applicationResult, Localizer, user.UserName));
        }
        public sealed class GetCardsRequest
        {
            public Guid DeckId { get; set; }
            public IEnumerable<GetAllAvailableTags.ViewModel> RequiredTags { get; set; } = null!;
            public int HeapFilter { get; set; }
            public string TextFilter { get; set; } = null!;
            public int pageNo { get; set; }
            public int pageSize { get; set; }
        }
        public sealed class GetCardsViewModel
        {
            public GetCardsViewModel(SearchDeckCards.SearchResult applicationResult, IStringLocalizer localizer, string currentUser)
            {
                TotalNbCards = applicationResult.TotalNbCards;
                PageCount = applicationResult.PageCount;
                Cards = applicationResult.Cards.Select(card => new Card(card, localizer, currentUser));
            }
            public int TotalNbCards { get; }
            public int PageCount { get; }
            public IEnumerable<Card> Cards { get; }
            public sealed class Card
            {
                public Card(SearchDeckCards.SearchResultCard card, IStringLocalizer localizer, string currentUser)
                {
                    CardId = card.CardId;
                    FrontSide = card.FrontSide;
                    Tags = card.Tags;
                    Heap = DisplayServices.HeapName(card.Heap, localizer);
                    ExpiryUtcDate = card.ExpiryUtcDate;
                    Expired = card.Expired;
                    NbTimesInNotLearnedHeap = card.NbTimesInNotLearnedHeap;
                    BiggestHeapReached = card.BiggestHeapReached;
                    LastLearnUtcDate = card.LastLearnUtcDate;
                    AddToDeckUtcTime = card.AddToDeckUtcTime;
                    RemoveAlertMessage = localizer["RemoveAlertMessage"] + " " + Heap + "\n" + localizer["DateAddedToDeck"] + " ";
                    VisibleToCount = card.VisibleTo.Count();
                    if (VisibleToCount == 1)
                    {
                        var visibleToUser = card.VisibleTo.First();
                        if (visibleToUser != currentUser)
                            throw new ApplicationException($"Card visible to single user should be current user, is {visibleToUser}");
                        VisibleTo = localizer["YouOnly"].ToString();
                    }
                    else
                    {
                        if (VisibleToCount == 0)
                            VisibleTo = localizer["AllUsers"].ToString();
                        else
                            VisibleTo = string.Join(',', card.VisibleTo);
                    }
                }
                public Guid CardId { get; }
                public string FrontSide { get; }
                public IEnumerable<string> Tags { get; }
                public DateTime? ExpiryUtcDate { get; }
                public DateTime LastLearnUtcDate { get; }
                public DateTime AddToDeckUtcTime { get; }
                public bool Expired { get; }
                public string Heap { get; }
                public int NbTimesInNotLearnedHeap { get; }
                public int BiggestHeapReached { get; }
                public string RemoveAlertMessage { get; }
                public int VisibleToCount { get; }
                public string VisibleTo { get; }
            }
        }
        */
        #endregion
        #region GetUserDecksWithHeaps
        [HttpGet("GetUserDecksWithHeaps")]
        public async Task<IActionResult> GetUserDecksWithHeaps()
        {
            var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
            var decks = new GetUserDecksWithHeaps(dbContext).Run(userId);
            var result = decks.Select(deck =>
                new GetUserDecksWithHeapsViewModel(
                    deck.DeckId,
                    deck.Description,
                    HeapingAlgoNameFromId(deck.HeapingAlgorithmId),
                    HeapingAlgoDescriptionFromId(deck.HeapingAlgorithmId),
                    deck.CardCount,
                    deck.Heaps.OrderBy(heap => heap.HeapId),
                    this
                    )
            );
            return base.Ok(result);
        }
        public sealed class GetUserDecksWithHeapsViewModel
        {
            public GetUserDecksWithHeapsViewModel(Guid deckId, string description, string heapingAlgorithmName, string heapingAlgorithmDescription, int cardCount, IEnumerable<GetUserDecksWithHeaps.ResultHeapModel> heaps, ILocalized localizer)
            {
                DeckId = deckId;
                Description = description;
                HeapingAlgorithmName = heapingAlgorithmName;
                HeapingAlgorithmDescription = heapingAlgorithmDescription;
                CardCount = cardCount;
                Heaps = heaps.Select(heap => new GetUserDecksWithHeapsHeapViewModel(heap.HeapId, DisplayServices.HeapName(heap.HeapId, localizer), heap.TotalCardCount, heap.ExpiredCardCount, heap.NextExpiryUtcDate));
            }
            public Guid DeckId { get; }
            public string Description { get; }
            public string HeapingAlgorithmName { get; }
            public string HeapingAlgorithmDescription { get; }
            public int CardCount { get; }
            public IEnumerable<GetUserDecksWithHeapsHeapViewModel> Heaps { get; }
        }
        public sealed class GetUserDecksWithHeapsHeapViewModel
        {
            public GetUserDecksWithHeapsHeapViewModel(int id, string name, int totalCardCount, int expiredCardCount, DateTime nextExpiryUtcDate)
            {
                Id = id;
                Name = name;
                TotalCardCount = totalCardCount;
                ExpiredCardCount = expiredCardCount;
                NextExpiryUtcDate = nextExpiryUtcDate;
            }
            public int Id { get; }
            public string Name { get; }
            public int TotalCardCount { get; }
            public int ExpiredCardCount { get; }
            public DateTime NextExpiryUtcDate { get; }
        }
        #endregion
        #region GetUserDecksForDeletion
        [HttpGet("GetUserDecksForDeletion")]
        public async Task<IActionResult> GetUserDecksForDeletion()
        {
            var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
            var decks = new GetUserDecks(dbContext).Run(userId);
            var result = decks.Select(deck => new GetUserDecksForDeletionViewModel(deck.DeckId, deck.Description, deck.CardCount,
                Get("SureYouWantToDelete") + " " + deck.Description + " " + Get("AndLose") + " " + deck.CardCount + " " + Get("CardLearningInfo") + " " + Get("NoUndo")));
            return base.Ok(result);
        }
        public sealed class GetUserDecksForDeletionViewModel
        {
            public GetUserDecksForDeletionViewModel(Guid deckId, string description, int cardCount, string alertMessage)
            {
                DeckId = deckId;
                Description = description;
                CardCount = cardCount;
                AlertMessage = alertMessage;
            }
            public string Description { get; }
            public Guid DeckId { get; }
            public int CardCount { get; }
            public string AlertMessage { get; }
        }
        #endregion
        #region DeleteDeck
        [HttpDelete("DeleteDeck/{deckId}")] public async Task<IActionResult> DeleteDeck(Guid deckId) => Ok(await new DeleteDeck(dbContext).RunAsync(deckId));
        #endregion
        #region GetTagsOfDeck
        [HttpGet("GetTagsOfDeck/{deckId}")]
        public IActionResult GetTagsOfDeck(Guid deckId)
        {
            var applicationResult = new GetTagsOfDeck(dbContext).Run(deckId);
            return base.Ok(applicationResult.Select(resultModel => new GetTagsOfDeckViewModel(resultModel.TagId, resultModel.TagName)));
        }
        public sealed class GetTagsOfDeckViewModel
        {
            public GetTagsOfDeckViewModel(Guid tagId, string tagName)
            {
                this.TagId = tagId;
                this.TagName = tagName;
            }
            public Guid TagId { get; }
            public string TagName { get; }
        }
        #endregion
        #region GetHeapsOfDeck
        [HttpGet("GetHeapsOfDeck/{deckId}")]
        public IActionResult GetHeapsOfDeck(Guid deckId)
        {
            var applicationResult = new GetHeapsOfDeck(dbContext).Run(deckId);
            return base.Ok(applicationResult.OrderBy(heapId => heapId).Select(heapId => new GetHeapsOfDeckViewModel(heapId, DisplayServices.HeapName(heapId, this))));
        }
        public sealed class GetHeapsOfDeckViewModel
        {
            public GetHeapsOfDeckViewModel(int heapId, string heapName)
            {
                HeapId = heapId;
                HeapName = heapName;
            }
            public int HeapId { get; }
            public string HeapName { get; }
        }
        #endregion
    }
}
