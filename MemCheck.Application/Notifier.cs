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

            var notifsToSend = await dbContext.CardNotifications.Where(notif => notif.UserId == userId && notif.LastNotificationUtcDate < DateTime.UtcNow).ToListAsync();
            var endOfRequest = DateTime.UtcNow;





            return new NotifierResult(registeredCardCount, notifsToSend.Select(notif => new CardVersion(notif.CardId)));
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
            public CardVersion(Guid cardId)
            {
                CardId = cardId;
            }
            public Guid CardId { get; }
        }
        #endregion
    }
}