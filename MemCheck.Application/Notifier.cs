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
        public async Task<ImmutableArray<CardVersionNotification>> GetNotificationsAsync(Guid userId)
        {
            var notifsToSend = await dbContext.CardNotifications.Where(notif => notif.UserId == userId && notif.LastNotificationUtcDate < DateTime.UtcNow).ToListAsync();
            var endOfRequest = DateTime.UtcNow;





            return notifsToSend.Select(n => new CardVersionNotification(n.CardId)).ToImmutableArray();
        }
        #region Result classes
        public class CardVersionNotification
        {
            public CardVersionNotification(Guid cardId)
            {
                CardId = cardId;
            }
            public Guid CardId { get; }
        }
        #endregion
    }
}