using MemCheck.Database;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Immutable;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;

namespace MemCheck.Application.Notifying
{
    internal sealed class UserCardDeletionsNotifier
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public UserCardDeletionsNotifier(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public ImmutableArray<CardDeletion> Run(MemCheckUser user)
        {
            //It is a little strange to keep checking for deleted cards when the user has been notified of their deletion. But I'm not clear right now about what to do in case a card is undeleted

            var deletedCards = from previousVersion in dbContext.CardPreviousVersions.Include(pv => pv.UsersWithView)
                               join cardNotif in dbContext.CardNotifications
                               on previousVersion.Card equals cardNotif.CardId
                               where (cardNotif.UserId == user.Id) && (previousVersion.VersionType == CardPreviousVersionType.Deletion) && (previousVersion.VersionUtcDate > cardNotif.LastNotificationUtcDate)
                               select new { previousVersion.FrontSide, previousVersion.VersionCreator, previousVersion.VersionUtcDate, previousVersion.VersionDescription, previousVersion.UsersWithView };

            return deletedCards.Select(cardToReport =>
              new CardDeletion(
                  cardToReport.FrontSide,
                  cardToReport.VersionCreator.UserName,
                  cardToReport.VersionUtcDate,
                  cardToReport.VersionDescription,
                  !cardToReport.UsersWithView.Any() || cardToReport.UsersWithView.Any(u => u.AllowedUserId == user.Id)
              )
          ).ToImmutableArray();
        }
    }
}