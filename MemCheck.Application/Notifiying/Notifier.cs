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
        public const int MaxLengthForTextFields = 150;
        #endregion
        #region Private methods
        private async Task<UserNotifications> GetUserNotificationsAsync(MemCheckUser user, DateTime now)
        {
            var registeredCardCount = await userCardSubscriptionCounter.RunAsync(user);
            var cardVersions = await userCardVersionsNotifier.RunAsync(user, now);
            var cardDeletions = await userCardDeletionsNotifier.RunAsync(user, now);

            return new UserNotifications(
                user.UserName,
                user.Email,
                registeredCardCount,
                cardVersions,
                cardDeletions
                );
        }
        #endregion
        public Notifier(MemCheckDbContext dbContext) : this(new UserCardSubscriptionCounter(dbContext), new UserCardVersionsNotifier(dbContext), new UserCardDeletionsNotifier(dbContext), new UsersToNotifyGetter(dbContext))
        {
        }
        internal Notifier(IUserCardSubscriptionCounter userCardSubscriptionCounter, IUserCardVersionsNotifier userCardVersionsNotifier, IUserCardDeletionsNotifier userCardDeletionsNotifier, IUsersToNotifyGetter usersToNotifyGetter)
        {
            this.userCardSubscriptionCounter = userCardSubscriptionCounter;
            this.userCardVersionsNotifier = userCardVersionsNotifier;
            this.userCardDeletionsNotifier = userCardDeletionsNotifier;
            this.usersToNotifyGetter = usersToNotifyGetter;
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
            public UserNotifications(string userName, string userEmail, int registeredCardCount, IEnumerable<CardVersion> cardVersions, IEnumerable<CardDeletion> deletedCards)
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
            public ImmutableArray<CardDeletion> DeletedCards { get; }
        }
        #endregion
    }
}