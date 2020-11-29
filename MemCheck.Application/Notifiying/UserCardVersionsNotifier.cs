using MemCheck.Database;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Immutable;
using MemCheck.Domain;
using System;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace MemCheck.Application.Notifying
{
    internal interface IUserCardVersionsNotifier
    {
        public Task<ImmutableArray<CardVersion>> RunAsync(Guid userId, DateTime? now = null);
    }
    internal sealed class UserCardVersionsNotifier : IUserCardVersionsNotifier
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public UserCardVersionsNotifier(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task<ImmutableArray<CardVersion>> RunAsync(Guid userId, DateTime? now = null)
        {
            var cardVersions = dbContext.Cards.Include(card => card.UsersWithView)
                .Join(
                dbContext.CardNotifications.Where(cardNotif => cardNotif.UserId == userId),
                card => card.Id,
                cardNotif => cardNotif.CardId,
                (card, cardNotif) => new { card, cardNotif }
                )
                .Where(cardAndNotif => cardAndNotif.card.VersionUtcDate > cardAndNotif.cardNotif.LastNotificationUtcDate);

            now = now ?? DateTime.UtcNow;
            foreach (var cardVersion in cardVersions)
                cardVersion.cardNotif.LastNotificationUtcDate = now.Value;

            var result = cardVersions.Select(cardToReport =>
                               new CardVersion(
                                   cardToReport.card.Id,
                                   cardToReport.card.FrontSide,
                                   cardToReport.card.VersionCreator.UserName,
                                   cardToReport.card.VersionUtcDate,
                                   cardToReport.card.VersionDescription,
                                   !cardToReport.card.UsersWithView.Any() || cardToReport.card.UsersWithView.Any(u => u.UserId == userId)
                               )
                        ).ToImmutableArray();

            await dbContext.SaveChangesAsync();

            return result;
        }
    }
}