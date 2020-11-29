using MemCheck.Database;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Immutable;
using MemCheck.Domain;
using System;

namespace MemCheck.Application.Notifying
{
    public sealed class Notifier
    {
        #region Fields
        private readonly IUserCardSubscriptionCounter userCardSubscriptionCounter;
        private readonly IUserCardVersionsNotifier userCardVersionsNotifier;
        private readonly IUserCardDeletionsNotifier userCardDeletionsNotifier;
        private readonly IUsersToNotifyGetter usersToNotifyGetter;
        private readonly IUserLastNotifDateUpdater userLastNotifDateUpdater;
        public const int MaxLengthForTextFields = 150;
        #endregion
        #region Private methods
        private async Task<UserNotifications> GetUserNotificationsAsync(MemCheckUser user, DateTime now)
        {
            var subscribedCardCount = await userCardSubscriptionCounter.RunAsync(user.Id);
            var cardVersions = await userCardVersionsNotifier.RunAsync(user, now);
            var cardDeletions = await userCardDeletionsNotifier.RunAsync(user.Id, now);

            await userLastNotifDateUpdater.RunAsync(user.Id, now);

            return new UserNotifications(
                user.UserName,
                user.Email,
                subscribedCardCount,
                cardVersions,
                cardDeletions
                );
        }
        #endregion
        public Notifier(MemCheckDbContext dbContext) : this(new UserCardSubscriptionCounter(dbContext), new UserCardVersionsNotifier(dbContext), new UserCardDeletionsNotifier(dbContext), new UsersToNotifyGetter(dbContext), new UserLastNotifDateUpdater(dbContext))
        {
        }
        internal Notifier(IUserCardSubscriptionCounter userCardSubscriptionCounter, IUserCardVersionsNotifier userCardVersionsNotifier, IUserCardDeletionsNotifier userCardDeletionsNotifier, IUsersToNotifyGetter usersToNotifyGetter, IUserLastNotifDateUpdater userLastNotifDateUpdater)
        {
            this.userCardSubscriptionCounter = userCardSubscriptionCounter;
            this.userCardVersionsNotifier = userCardVersionsNotifier;
            this.userCardDeletionsNotifier = userCardDeletionsNotifier;
            this.usersToNotifyGetter = usersToNotifyGetter;
            this.userLastNotifDateUpdater = userLastNotifDateUpdater;
        }
        public async Task<NotifierResult> GetNotificationsAndUpdateLastNotifDatesAsync(DateTime? now = null)
        {
            now = now ?? DateTime.UtcNow;
            var users = usersToNotifyGetter.Run(now);
            var userNotifications = new List<UserNotifications>();
            foreach (var user in users)
                userNotifications.Add(await GetUserNotificationsAsync(user, now.Value));
            return new NotifierResult(userNotifications.ToImmutableArray());
        }
        #region Result classes
        public class NotifierResult
        {
            public NotifierResult(ImmutableArray<UserNotifications> userNotifications)
            {
                UserNotifications = userNotifications;
            }
            public ImmutableArray<UserNotifications> UserNotifications { get; }
        }
        public class UserNotifications
        {
            public UserNotifications(string userName, string userEmail, int subscribedCardCount, IEnumerable<CardVersion> cardVersions, IEnumerable<CardDeletion> deletedCards)
            {
                UserName = userName;
                UserEmail = userEmail;
                SubscribedCardCount = subscribedCardCount;
                CardVersions = cardVersions.ToImmutableArray();
                DeletedCards = deletedCards.ToImmutableArray();
            }
            public string UserName { get; }
            public string UserEmail { get; }
            public int SubscribedCardCount { get; }
            public ImmutableArray<CardVersion> CardVersions { get; }
            public ImmutableArray<CardDeletion> DeletedCards { get; }
        }
        #endregion
    }
}