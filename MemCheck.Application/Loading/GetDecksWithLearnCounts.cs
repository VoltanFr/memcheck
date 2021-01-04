using MemCheck.Application.Heaping;
using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemCheck.Application.Loading
{
    public sealed class GetDecksWithLearnCounts
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        #region Private methods
        private Result GetDeck(Guid deckId, int heapingAlgorithmId, string description, DateTime todayOnClientSide, DateTime now)
        {
            var allCards = dbContext.CardsInDecks.AsNoTracking().Where(cardInDeck => cardInDeck.DeckId == deckId)
                .Select(cardInDeck => new { cardInDeck.CurrentHeap, cardInDeck.LastLearnUtcTime, cardInDeck.DeckId })
                .ToList();
            var groups = allCards.ToLookup(card => card.CurrentHeap == 0);
            var heapingAlgorithm = HeapingAlgorithms.Instance.FromId(heapingAlgorithmId);
            var expiredCardCount = 0;
            var nextExpiryUTCDate = DateTime.MaxValue;
            var expiringTodayCount = 0;
            var expiringTomorrowCount = 0;
            var expiring5NextDaysCount = 0;
            var tomorrowDate = todayOnClientSide.AddDays(1);
            var debugInfo = new StringBuilder();
            debugInfo.Append($"todayOnClientSide: {todayOnClientSide}<br/>");
            foreach (var card in groups[false])
            {
                var expiryDate = heapingAlgorithm.ExpiryUtcDate(card.CurrentHeap, card.LastLearnUtcTime);
                if (expiryDate <= now)
                    expiredCardCount++;
                else
                {
                    if (expiryDate.Date == todayOnClientSide)
                    {
                        debugInfo.Append($"Expiring {expiryDate}<br/>");
                        expiringTodayCount++;
                    }
                    else
                    if (expiryDate.Date == tomorrowDate)
                        expiringTomorrowCount++;
                    else
                    if (expiryDate.Date - now.Date <= TimeSpan.FromDays(6))
                        expiring5NextDaysCount++;
                    if (expiryDate < nextExpiryUTCDate)
                        nextExpiryUTCDate = expiryDate;
                }
            }
            return new Result(deckId, description, groups[true].Count(), expiredCardCount, allCards.Count, expiringTodayCount, expiringTomorrowCount, expiring5NextDaysCount, nextExpiryUTCDate, debugInfo.ToString());
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
            var todayOnClientSide = now.Value.Date;//.AddMinutes(-request.ClientSideTimezoneOffset).Date;
            var decks = await dbContext.Decks.AsNoTracking().Where(deck => deck.Owner.Id == request.UserId).Select(deck => new { deck.Id, deck.Description, deck.HeapingAlgorithmId }).ToListAsync();
            return decks.Select(deck => GetDeck(deck.Id, deck.HeapingAlgorithmId, deck.Description, todayOnClientSide, now.Value)).ToList();
        }
        #region Request & Result
        public sealed record Request(Guid UserId, int ClientSideTimezoneOffset)
        {
            public async Task CheckValidityAsync(MemCheckDbContext dbContext)
            {
                QueryValidationHelper.CheckNotReservedGuid(UserId);

                if (!await dbContext.Users.AnyAsync(u => u.Id == UserId))
                    throw new RequestInputException("Bad user");
            }
        }
        public sealed record Result(
            Guid Id,
            string Description,
            int UnknownCardCount,
            int ExpiredCardCount,
            int CardCount,
            int ExpiringTodayCount,
            int ExpiringTomorrowCount,
            int Expiring5NextDaysCount,
            DateTime NextExpiryUTCDate,
            string DebugInfo);
        //NextExpiryUTCDate is DateTime.MaxValue if all cards are unknown
        //Expiring5NextDaysCount means among the 5 days after tomorrow
        #endregion
    }
}
