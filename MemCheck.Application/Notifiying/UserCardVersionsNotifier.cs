using MemCheck.Database;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Immutable;
using MemCheck.Domain;

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
        public ImmutableArray<CardVersion> Run(MemCheckUser user)
        {
            var cardVersions = from card in dbContext.Cards
                               join cardNotif in dbContext.CardNotifications
                               on card.Id equals cardNotif.CardId
                               where cardNotif.UserId == user.Id && card.VersionUtcDate > cardNotif.LastNotificationUtcDate
                               select card;

            return cardVersions.Select(cardToReport =>
                new CardVersion(
                    cardToReport.Id,
                    cardToReport.FrontSide,
                    cardToReport.VersionCreator.UserName,
                    cardToReport.VersionUtcDate,
                    cardToReport.VersionDescription,
                    !cardToReport.UsersWithView.Any() || cardToReport.UsersWithView.Any(u => u.UserId == user.Id)
                )
            ).ToImmutableArray();
        }
    }
}