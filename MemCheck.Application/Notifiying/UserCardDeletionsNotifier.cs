using MemCheck.Database;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Immutable;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace MemCheck.Application.Notifying
{
    internal interface IUserCardDeletionsNotifier
    {
        public Task<ImmutableArray<RegisteredCardDeletion>> RunAsync(Guid userId, DateTime? now = null);
    }
    internal sealed class UserCardDeletionsNotifier : IUserCardDeletionsNotifier
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public UserCardDeletionsNotifier(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task<ImmutableArray<RegisteredCardDeletion>> RunAsync(Guid userId, DateTime? now = null)
        {
            //It is a little strange to keep checking for deleted cards when the user has been notified of their deletion. But I'm not clear right now about what to do in case a card is undeleted

            var deletedCards = dbContext.CardPreviousVersions.Include(card => card.UsersWithView)
                .Join(
                    dbContext.CardNotifications.Where(cardNotif => cardNotif.UserId == userId),
                    previousVersion => previousVersion.Card,
                    cardNotif => cardNotif.CardId,
                    (previousVersion, cardNotif) => new { previousVersion, cardNotif }
                )
                .Where(cardAndNotif => (cardAndNotif.previousVersion.VersionType == CardPreviousVersionType.Deletion) && cardAndNotif.previousVersion.VersionUtcDate > cardAndNotif.cardNotif.LastNotificationUtcDate);

            now = now ?? DateTime.UtcNow;
            foreach (var cardVersion in deletedCards)
                cardVersion.cardNotif.LastNotificationUtcDate = now.Value;

            var result = deletedCards.Select(cardToReport =>
                             new RegisteredCardDeletion(
                                 cardToReport.previousVersion.FrontSide,
                                 cardToReport.previousVersion.VersionCreator.UserName,
                                 cardToReport.previousVersion.VersionUtcDate,
                                 cardToReport.previousVersion.VersionDescription,
                                 !cardToReport.previousVersion.UsersWithView.Any() || cardToReport.previousVersion.UsersWithView.Any(u => u.AllowedUserId == userId)
                             )
                      ).ToImmutableArray();

            await dbContext.SaveChangesAsync();

            return result;
        }
    }
}