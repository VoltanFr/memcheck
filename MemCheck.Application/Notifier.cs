using MemCheck.Database;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Collections.Immutable;

namespace MemCheck.Application
{
    public sealed class Notifier
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public Notifier(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task<NotifierResult> GetNotificationsAsync(Guid userId)
        {
            var registeredCardCount = await dbContext.CardNotifications.Where(notif => notif.UserId == userId).CountAsync();

            var cardsToReport = await dbContext.CardNotifications
                .Include(notif => notif.Card)
                .Where(notif => notif.UserId == userId && notif.LastNotificationUtcDate < notif.Card.VersionUtcDate)
                .ToListAsync();
            var endOfRequest = DateTime.UtcNow;

            foreach (var cardToReport in cardsToReport)
                cardToReport.LastNotificationUtcDate = endOfRequest;

            await dbContext.SaveChangesAsync();

            return new NotifierResult(registeredCardCount, cardsToReport.Select(cardToReport => new CardVersion(cardToReport.CardId, cardToReport.Card.FrontSide)));
        }
        #region Result classes
        public class NotifierResult
        {

            public NotifierResult(int registeredCardCount, IEnumerable<CardVersion> cardVersions)
            {
                RegisteredCardCount = registeredCardCount;
                CardVersions = cardVersions.ToImmutableArray();
            }
            public int RegisteredCardCount { get; }
            public ImmutableArray<CardVersion> CardVersions { get; }
        }
        public class CardVersion
        {
            public CardVersion(Guid cardId, string frontSide)
            {
                CardId = cardId;
                FrontSide = frontSide;
            }
            public Guid CardId { get; }
            public string FrontSide { get; }
        }
        #endregion
    }
}