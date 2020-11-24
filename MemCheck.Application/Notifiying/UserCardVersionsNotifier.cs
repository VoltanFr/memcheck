using MemCheck.Database;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Immutable;
using MemCheck.Domain;
using System;
using Microsoft.EntityFrameworkCore;

namespace MemCheck.Application.Notifying
{
    internal sealed class UserCardVersionsNotifier
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public UserCardVersionsNotifier(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public ImmutableArray<CardVersion> Run(MemCheckUser user, DateTime? now = null)
        {
            var cardVersions = dbContext.Cards.Include(card => card.UsersWithView)
                .Join(
                dbContext.CardNotifications.Where(cardNotif => cardNotif.UserId == user.Id),
                card => card.Id,
                cardNotif => cardNotif.CardId,
                (card, cardNotif) => new { card, cardNotif }
                )
                .Where(cardAndNotif => cardAndNotif.card.VersionUtcDate > cardAndNotif.cardNotif.LastNotificationUtcDate);

            now = now ?? DateTime.UtcNow;
            foreach (var cardVersion in cardVersions)
                cardVersion.cardNotif.LastNotificationUtcDate = now.Value;

            return cardVersions.Select(cardToReport =>
                new CardVersion(
                    cardToReport.card.Id,
                    cardToReport.card.FrontSide,
                    cardToReport.card.VersionCreator.UserName,
                    cardToReport.card.VersionUtcDate,
                    cardToReport.card.VersionDescription,
                    !cardToReport.card.UsersWithView.Any() || cardToReport.card.UsersWithView.Any(u => u.UserId == user.Id)
                )
            ).ToImmutableArray();
        }
    }
}