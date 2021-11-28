using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Decks
{
    public sealed class GetUserDecksWithHeapsAndTags : RequestRunner<GetUserDecksWithHeapsAndTags.Request, IEnumerable<GetUserDecksWithHeapsAndTags.Result>>
    {
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
        public GetUserDecksWithHeapsAndTags(CallContext callContext) : base(callContext)
        {
        }
        protected override async Task<ResultWithMetrologyProperties<IEnumerable<Result>>> DoRunAsync(Request request)
        {
            var decks = DbContext.Decks.AsNoTracking().Where(deck => deck.Owner.Id == request.UserId).OrderBy(deck => deck.Description).Select(deck => new { deck.Id, deck.Description }).ToList();

            var result = new List<Result>();

            foreach (var deck in decks)
            {
                var heaps = await DbContext.CardsInDecks.AsNoTracking().Where(cardInDeck => cardInDeck.DeckId == deck.Id).Select(cardInDeck => cardInDeck.CurrentHeap).Distinct().ToListAsync();
                var tags = await new GetTagsOfDeck(DbContext).RunAsync(new GetTagsOfDeck.Request(request.UserId, deck.Id));
                result.Add(new Result(deck.Id, deck.Description, heaps, tags.Select(tag => new ResultTag(tag.TagId, tag.TagName))));
            }

            return new ResultWithMetrologyProperties<IEnumerable<Result>>(result, ("DeckCount", result.Count.ToString()));
        }
        #region Request & Result
        public sealed record Request(Guid UserId) : IRequest
        {
            public async Task CheckValidityAsync(CallContext callContext)
            {
                await QueryValidationHelper.CheckUserExistsAsync(callContext.DbContext, UserId);
            }
        }
        public sealed record Result(Guid DeckId, string Description, IEnumerable<int> Heaps, IEnumerable<ResultTag> Tags);
        public sealed record ResultTag(Guid TagId, string TagName);
        #endregion
    }
}
