using MemCheck.Application;
using MemCheck.Application.Decks;
using MemCheck.Application.Heaping;
using MemCheck.Application.QueryValidation;
using MemCheck.Basics;
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
            var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
            var appRequest = new CreateDeck.Request(userId, request.Description.Trim(), request.HeapingAlgorithmId);
            await new CreateDeck(dbContext).RunAsync(appRequest, this);
            return Ok();
        }
        public sealed class CreateRequest
        {
            public string Description { get; set; } = null!;
            public int HeapingAlgorithmId { get; set; }
        }
        #endregion
        #region Update
        [HttpPost("Update")]
        async public Task<IActionResult> Update([FromBody] UpdateRequest request)
        {
            CheckBodyParameter(request);
            var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
            var appRequest = new UpdateDeck.Request(userId, request.DeckId, request.Description.Trim(), request.HeapingAlgorithmId);
            return Ok(await new UpdateDeck(dbContext).RunAsync(appRequest, this));
        }
        public sealed class UpdateRequest
        {
            public Guid DeckId { get; set; }
            public string Description { get; set; } = null!;
            public int HeapingAlgorithmId { get; set; }
        }
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
        [HttpDelete("DeleteDeck/{deckId}")]
        public async Task<IActionResult> DeleteDeck(Guid deckId)
        {
            var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
            await new DeleteDeck(dbContext).RunAsync(new DeleteDeck.Request(userId, deckId));
            return Ok();
        }
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
    }
}
