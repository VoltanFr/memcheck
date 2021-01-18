using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application
{
    public sealed class GetUserDecksWithHeapsAndTags
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        #region Private class HeapInfo
        private sealed class HeapInfo
        {
            public HeapInfo(int heapId)
            {
                HeapId = heapId;
                TotalCardCount = 0;
                ExpiredCardCount = 0;
                NextExpiryUtcDate = DateTime.MaxValue;
            }
            public int HeapId { get; set; }
            public int TotalCardCount { get; set; }
            public int ExpiredCardCount { get; set; }
            public DateTime NextExpiryUtcDate { get; set; }
        }
        #endregion
        public GetUserDecksWithHeapsAndTags(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task<IEnumerable<ResultModel>> RunAsync(Guid userId)
        {
            if (QueryValidationHelper.IsReservedGuid(userId))
                throw new RequestInputException($"Invalid user id {userId}");

            var decks = dbContext.Decks.AsNoTracking().Where(deck => deck.Owner.Id == userId).OrderBy(deck => deck.Description).Select(deck => new { deck.Id, deck.Description }).ToList();

            var result = new List<ResultModel>();

            foreach (var deck in decks)
            {
                var heaps = await dbContext.CardsInDecks.AsNoTracking().Where(cardInDeck => cardInDeck.DeckId == deck.Id).Select(cardInDeck => cardInDeck.CurrentHeap).Distinct().ToListAsync();
                var tags = new GetTagsOfDeck(dbContext).Run(deck.Id);
                result.Add(new ResultModel(deck.Id, deck.Description, heaps, tags.Select(tag => new ResultTagModel(tag.TagId, tag.TagName))));
            }

            return result;
        }
        #region Result classes
        public sealed class ResultModel
        {
            public ResultModel(Guid deckId, string description, IEnumerable<int> heaps, IEnumerable<ResultTagModel> tags)
            {
                DeckId = deckId;
                Description = description;
                Heaps = heaps;
                Tags = tags;
            }
            public Guid DeckId { get; set; }
            public string Description { get; }
            public IEnumerable<int> Heaps { get; }
            public IEnumerable<ResultTagModel> Tags { get; }
        }
        public sealed class ResultTagModel
        {
            public ResultTagModel(Guid tagId, string tagName)
            {
                TagId = tagId;
                TagName = tagName;
            }
            public Guid TagId { get; }
            public string TagName { get; }
        }
        #endregion
    }
}
