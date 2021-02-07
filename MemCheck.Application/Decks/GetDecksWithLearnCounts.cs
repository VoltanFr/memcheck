using MemCheck.Application.Heaping;
using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Decks
{
    public sealed class GetDecksWithLearnCounts
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        private readonly TimeSpan oneHour = TimeSpan.FromHours(1);
        private readonly TimeSpan twentyFiveHours = TimeSpan.FromHours(25);
        private readonly TimeSpan fourDays = TimeSpan.FromDays(4);
        #endregion
        #region Private methods
        private Result GetDeck(Guid deckId, int heapingAlgorithmId, string description, DateTime now)
        {
            var allCards = dbContext.CardsInDecks.AsNoTracking()
                .Where(cardInDeck => cardInDeck.DeckId == deckId)
                .Select(cardInDeck => new { cardInDeck.CurrentHeap, cardInDeck.LastLearnUtcTime, cardInDeck.DeckId, cardInDeck.ExpiryUtcTime })
                .ToImmutableArray();
            var groups = allCards.ToLookup(card => card.CurrentHeap == 0);
            var expiredCardCount = 0;
            var nextExpiryUTCDate = DateTime.MaxValue;
            var expiringNextHourCount = 0;
            var expiringFollowing24hCount = 0;
            var expiringFollowing3DaysCount = 0;
            foreach (var card in groups[false])
            {
                if (card.ExpiryUtcTime <= now)
                    expiredCardCount++;
                else
                {
                    var distanceToNow = card.ExpiryUtcTime - now;
                    if (distanceToNow <= oneHour)
                        expiringNextHourCount++;
                    else
                    {
                        if (distanceToNow <= twentyFiveHours)
                            expiringFollowing24hCount++;
                        else
                        {
                            if (distanceToNow <= fourDays)
                                expiringFollowing3DaysCount++;
                        }
                    }
                    if (card.ExpiryUtcTime < nextExpiryUTCDate)
                        nextExpiryUTCDate = card.ExpiryUtcTime;
                }
            }
            return new Result(deckId, description, groups[true].Count(), expiredCardCount, allCards.Length, expiringNextHourCount, expiringFollowing24hCount, expiringFollowing3DaysCount, nextExpiryUTCDate);
        }
        #endregion
        public GetDecksWithLearnCounts(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task<IEnumerable<Result>> RunAsync(Request request, DateTime? now = null)
        {
            await request.CheckValidityAsync(dbContext);
            if (now == null)
                now = DateTime.UtcNow;
            var decks = await dbContext.Decks.AsNoTracking().Where(deck => deck.Owner.Id == request.UserId).Select(deck => new { deck.Id, deck.Description, deck.HeapingAlgorithmId }).ToListAsync();
            return decks.Select(deck => GetDeck(deck.Id, deck.HeapingAlgorithmId, deck.Description, now.Value)).ToList();
        }
        #region Request & Result
        public sealed record Request(Guid UserId)
        {
            public async Task CheckValidityAsync(MemCheckDbContext dbContext)
            {
                await QueryValidationHelper.CheckUserExistsAsync(dbContext, UserId);
            }
        }
        public sealed record Result(
            Guid Id,
            string Description,
            int UnknownCardCount,
            int ExpiredCardCount,
            int CardCount,
            int ExpiringNextHourCount,
            int ExpiringFollowing24hCount,
            int ExpiringFollowing3DaysCount,
            DateTime NextExpiryUTCDate);
        //NextExpiryUTCDate is DateTime.MaxValue if all cards are unknown
        #endregion
    }
}
