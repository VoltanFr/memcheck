using MemCheck.Application.Heaping;
using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Loading
{
    public sealed class GetDecksWithLearnCounts
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        #region Private methods
        private Result GetDeck(Guid deckId, int heapingAlgorithmId, string description)
        {
            var allCards = dbContext.CardsInDecks.AsNoTracking().Where(cardInDeck => cardInDeck.DeckId == deckId)
                .Select(cardInDeck => new { cardInDeck.CurrentHeap, cardInDeck.LastLearnUtcTime, cardInDeck.DeckId })
                .ToList();
            var groups = allCards.ToLookup(card => card.CurrentHeap == 0);
            HeapingAlgorithm heapingAlgorithm = HeapingAlgorithms.Instance.FromId(heapingAlgorithmId);
            var expiredCardCount = 0;
            var nextExpiryUTCDate = DateTime.MaxValue;
            foreach (var card in groups[false])
            {
                var expiryDate = heapingAlgorithm.ExpiryUtcDate(card.CurrentHeap, card.LastLearnUtcTime);
                if (expiryDate <= DateTime.UtcNow)
                    expiredCardCount++;
                else
                    if (expiryDate < nextExpiryUTCDate)
                    nextExpiryUTCDate = expiryDate;
            }
            return new Result(deckId, description, groups[true].Count(), expiredCardCount, allCards.Count, false, nextExpiryUTCDate);
        }
        #endregion
        public GetDecksWithLearnCounts(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task<IEnumerable<Result>> RunAsync(Request request)
        {
            request.CheckValidityAsync(dbContext);

            var decks = await dbContext.Decks.AsNoTracking().Where(deck => deck.Owner.Id == request.UserId).Select(deck => new { deck.Id, deck.Description, deck.HeapingAlgorithmId }).ToListAsync();
            return decks.Select(deck => GetDeck(deck.Id, deck.HeapingAlgorithmId, deck.Description)).ToList();
        }
        #region Request & Result
        public sealed record Request(Guid UserId)
        {
            public void CheckValidityAsync(MemCheckDbContext dbContext)
            {
                QueryValidationHelper.CheckNotReservedGuid(UserId);

                if (!dbContext.Users.Any(u => u.Id == UserId))
                    throw new RequestInputException("Bad user");
            }
        }
        public sealed record Result(Guid Id, string Description, int UnknownCardCount, int ExpiredCardCount, int CardCount, bool IsEmpty, DateTime NextExpiryUTCDate);
        #endregion
    }
}
