using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Decks
{
    public sealed class GetUserDecksWithHeapsAndTags
    {
        #region Fields
        private readonly CallContext callContext;
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
        public GetUserDecksWithHeapsAndTags(CallContext callContext)
        {
            this.callContext = callContext;
        }
        public async Task<IEnumerable<Result>> RunAsync(Request request)
        {
            await request.CheckValidityAsync(callContext.DbContext);

            var decks = callContext.DbContext.Decks.AsNoTracking().Where(deck => deck.Owner.Id == request.UserId).OrderBy(deck => deck.Description).Select(deck => new { deck.Id, deck.Description }).ToList();

            var result = new List<Result>();

            foreach (var deck in decks)
            {
                var heaps = await callContext.DbContext.CardsInDecks.AsNoTracking().Where(cardInDeck => cardInDeck.DeckId == deck.Id).Select(cardInDeck => cardInDeck.CurrentHeap).Distinct().ToListAsync();
                var tags = await new GetTagsOfDeck(callContext).RunAsync(new GetTagsOfDeck.Request(request.UserId, deck.Id));
                result.Add(new Result(deck.Id, deck.Description, heaps, tags.Select(tag => new ResultTag(tag.TagId, tag.TagName))));
            }

            callContext.TelemetryClient.TrackEvent("GetUserDecksWithHeapsAndTags", ("DeckCount", result.Count.ToString()));
            return result;
        }
        #region Request & Result
        public sealed record Request(Guid UserId)
        {
            public async Task CheckValidityAsync(MemCheckDbContext dbContext)
            {
                await QueryValidationHelper.CheckUserExistsAsync(dbContext, UserId);
            }
        }
        public sealed record Result(Guid DeckId, string Description, IEnumerable<int> Heaps, IEnumerable<ResultTag> Tags);
        public sealed record ResultTag(Guid TagId, string TagName);
        #endregion
    }
}
