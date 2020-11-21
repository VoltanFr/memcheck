using MemCheck.Database;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Collections.Immutable;
using MemCheck.Domain;

namespace MemCheck.Application.Notifying
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
        private async Task<UserNotifications> GetUserNotificationsAsync(MemCheckUser user)
        {
            //To do: check that we don't report a card the user does not have right to read
            //It is a little strange to keep checking for deleted cards when the user has been notified of their deletion. But I'm not clear right now about what to do in case a card is undeleted

            var registeredCardCount = await dbContext.CardNotifications.Where(notif => notif.UserId == user.Id).CountAsync();

            //var registeredCardsForUser = await dbContext.CardNotifications.Where(notif => notif.UserId == userId).ToDictionaryAsync(notif => notif.CardId, notif => notif);

            var cardVersions = from card in dbContext.Cards
                               join cardNotif in dbContext.CardNotifications
                               on card.Id equals cardNotif.CardId
                               where cardNotif.UserId == user.Id && card.VersionUtcDate > cardNotif.LastNotificationUtcDate
                               select card;

            var previousVersions = dbContext.CardPreviousVersions.ToList();

            var deletedVersions = dbContext.CardPreviousVersions.Where(pv => pv.VersionType == CardPreviousVersionType.Deletion).ToList();
            var lastDeleted = deletedVersions.Last();

            var notifs = dbContext.CardNotifications.ToList();

            var deletedCards = from previousVersion in dbContext.CardPreviousVersions
                               join cardNotif in dbContext.CardNotifications
                               on previousVersion.Card equals cardNotif.CardId
                               where (cardNotif.UserId == user.Id)// && (previousVersion.VersionType.Equals(CardPreviousVersionType.Deletion)) //&& (previousVersion.VersionUtcDate > cardNotif.LastNotificationUtcDate)
                               select previousVersion;

            //var cardVersions = await dbContext.Cards.Where(card => registeredCardsForUser.ContainsKey(card.Id) && card.VersionUtcDate > registeredCardsForUser[card.Id].LastNotificationUtcDate).ToListAsync();
            //var deletedCards = await dbContext.CardPreviousVersions.Where(deletedCard => registeredCardsForUser.ContainsKey(deletedCard.Card) && deletedCard.VersionType == CardPreviousVersionType.Deletion && deletedCard.VersionUtcDate > registeredCardsForUser[deletedCard.Card].LastNotificationUtcDate).ToListAsync();

            var endOfRequest = DateTime.UtcNow;

            //foreach (var registeredCard in registeredCardsForUser.Values)
            //    registeredCard.LastNotificationUtcDate = endOfRequest;

            await dbContext.SaveChangesAsync();

            return new UserNotifications(
                user.UserName,
                user.Email,
                registeredCardCount,
                cardVersions.Select(cardToReport => new CardVersion(cardToReport.Id, cardToReport.FrontSide, cardToReport.VersionCreator.UserName, cardToReport.VersionUtcDate, cardToReport.VersionDescription)),
                deletedCards.Select(deletedCard => new DeletedCard(deletedCard.FrontSide, deletedCard.VersionCreator.UserName, deletedCard.VersionUtcDate, deletedCard.VersionDescription))
                );
        }
        public async Task<NotifierResult> GetNotificationsAsync()
        {
            var users = new UsersToNotifyGetter(dbContext).Run();
            var userNotifications = new List<UserNotifications>();
            foreach (var user in users)
                userNotifications.Add(await GetUserNotificationsAsync(user));
            return new NotifierResult(userNotifications);
        }
        #region Result classes
        public class NotifierResult
        {
            public NotifierResult(IEnumerable<UserNotifications> userNotifications)
            {
                UserNotifications = userNotifications;
            }
            public IEnumerable<UserNotifications> UserNotifications { get; }
        }
        public class UserNotifications
        {
            public UserNotifications(string userName, string userEmail, int registeredCardCount, IEnumerable<CardVersion> cardVersions, IEnumerable<DeletedCard> deletedCards)
            {
                UserName = userName;
                UserEmail = userEmail;
                RegisteredCardCount = registeredCardCount;
                CardVersions = cardVersions.ToImmutableArray();
                DeletedCards = deletedCards.ToImmutableArray();
            }
            public string UserName { get; }
            public string UserEmail { get; }
            public int RegisteredCardCount { get; }
            public ImmutableArray<CardVersion> CardVersions { get; }
            public ImmutableArray<DeletedCard> DeletedCards { get; }
        }
        public class CardVersion
        {
            public CardVersion(Guid cardId, string frontSide, string versionCreator, DateTime versionUtcDate, string versionDescription)
            {
                CardId = cardId;
                FrontSide = frontSide.Truncate(100, true);
                VersionCreator = versionCreator;
                VersionUtcDate = versionUtcDate;
                VersionDescription = versionDescription;
            }
            public Guid CardId { get; }
            public string FrontSide { get; }
            public string VersionCreator { get; }
            public DateTime VersionUtcDate { get; }
            public string VersionDescription { get; }
        }
        public class DeletedCard
        {
            public DeletedCard(string frontSide, string deletionAuthor, DateTime deletionUtcDate, string deletionDescription)
            {
                FrontSide = frontSide.Truncate(100, true);
                DeletionAuthor = deletionAuthor;
                DeletionUtcDate = deletionUtcDate;
                DeletionDescription = deletionDescription;
            }
            public string FrontSide { get; }
            public string DeletionAuthor { get; }
            public DateTime DeletionUtcDate { get; }
            public string DeletionDescription { get; }
        }
        #endregion
    }
    internal sealed class UsersToNotifyGetter
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public UsersToNotifyGetter(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public ImmutableArray<MemCheckUser> Run()
        {
            //Will be reviwed when we have the table of registration info
            var userList = dbContext.CardNotifications.Select(notif => notif.User).Distinct();
            return userList.ToImmutableArray();
        }
    }
}