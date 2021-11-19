using MemCheck.Database;
using MemCheck.Domain;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MemCheck.Application.Notifying
{
    public sealed class Notifier
    {
        #region Fields
        private readonly CallContext callContext;
        private readonly IUserCardSubscriptionCounter userCardSubscriptionCounter;
        private readonly IUserSearchSubscriptionLister userSearchSubscriptionLister;
        private readonly IUserCardVersionsNotifier userCardVersionsNotifier;
        private readonly IUserCardDeletionsNotifier userCardDeletionsNotifier;
        private readonly IUsersToNotifyGetter usersToNotifyGetter;
        private readonly IUserLastNotifDateUpdater userLastNotifDateUpdater;
        private readonly IUserSearchNotifier userSearchNotifier;
        private readonly List<string> performanceIndicators;
        public const int MaxLengthForTextFields = 150;
        public const int MaxCardsToReportPerSearch = 100;
        #endregion
        #region Private methods
        private async Task<UserNotifications> GetUserNotificationsAsync(MemCheckUser user)
        {
            var subscribedCardCount = await userCardSubscriptionCounter.RunAsync(user.Id);
            var cardVersions = await userCardVersionsNotifier.RunAsync(user.Id);
            var cardDeletions = await userCardDeletionsNotifier.RunAsync(user.Id);
            var subscribedSearches = await userSearchSubscriptionLister.RunAsync(user.Id);

            var searchNotifs = new List<UserSearchNotifierResult>();
            foreach (var subscribedSearch in subscribedSearches)
                searchNotifs.Add(await userSearchNotifier.RunAsync(subscribedSearch.Id));

            await userLastNotifDateUpdater.RunAsync(user.Id);

            return new UserNotifications(
                user.UserName,
                user.Email,
                subscribedCardCount,
                cardVersions,
                cardDeletions,
                searchNotifs
                );
        }
        #endregion
        public Notifier(CallContext callContext, List<string> performanceIndicators)
            : this(
                  callContext,
                  new UserCardSubscriptionCounter(callContext.DbContext, performanceIndicators),
                  new UserCardVersionsNotifier(callContext.DbContext, performanceIndicators),
                  new UserCardDeletionsNotifier(callContext, performanceIndicators),
                  new UsersToNotifyGetter(callContext.DbContext, performanceIndicators),
                  new UserLastNotifDateUpdater(callContext.DbContext, performanceIndicators, DateTime.UtcNow),
                  new UserSearchSubscriptionLister(callContext.DbContext, performanceIndicators),
                  new UserSearchNotifier(callContext.DbContext, MaxCardsToReportPerSearch, performanceIndicators),
                  performanceIndicators)
        {
        }
        internal Notifier(CallContext callContext, IUserCardSubscriptionCounter userCardSubscriptionCounter, IUserCardVersionsNotifier userCardVersionsNotifier, IUserCardDeletionsNotifier userCardDeletionsNotifier, IUsersToNotifyGetter usersToNotifyGetter, IUserLastNotifDateUpdater userLastNotifDateUpdater, IUserSearchSubscriptionLister userSearchSubscriptionLister, IUserSearchNotifier userSearchNotifier, List<string> performanceIndicators)
        {
            this.callContext = callContext;
            this.userCardSubscriptionCounter = userCardSubscriptionCounter;
            this.userCardVersionsNotifier = userCardVersionsNotifier;
            this.userCardDeletionsNotifier = userCardDeletionsNotifier;
            this.usersToNotifyGetter = usersToNotifyGetter;
            this.userLastNotifDateUpdater = userLastNotifDateUpdater;
            this.userSearchSubscriptionLister = userSearchSubscriptionLister;
            this.userSearchNotifier = userSearchNotifier;
            this.performanceIndicators = performanceIndicators;
        }
        public async Task<NotifierResult> GetNotificationsAndUpdateLastNotifDatesAsync(DateTime? now = null)
        {
            var chrono = Stopwatch.StartNew();
            now ??= DateTime.UtcNow;
            var users = usersToNotifyGetter.Run(now);
            var userNotifications = new List<UserNotifications>();
            foreach (var user in users)
                userNotifications.Add(await GetUserNotificationsAsync(user));
            performanceIndicators.Add($"Total Notifier execution time: {chrono.Elapsed}");
            var result = userNotifications.ToImmutableArray();
            callContext.TelemetryClient.TrackEvent("Notifier", ("ResultCount", result.Length.ToString()));
            return new NotifierResult(result);
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
        public record UserNotifications
        {
            public UserNotifications(string userName, string userEmail, int subscribedCardCount, IEnumerable<CardVersion> cardVersions, IEnumerable<CardDeletion> deletedCards, IEnumerable<UserSearchNotifierResult> searchNotificactions)
            {
                UserName = userName;
                UserEmail = userEmail;
                SubscribedCardCount = subscribedCardCount;
                CardVersions = cardVersions.ToImmutableArray();
                DeletedCards = deletedCards.ToImmutableArray();
                SearchNotificactions = searchNotificactions.ToImmutableArray();
            }
            public string UserName { get; }
            public string UserEmail { get; }
            public int SubscribedCardCount { get; }
            public ImmutableArray<CardVersion> CardVersions { get; }
            public ImmutableArray<CardDeletion> DeletedCards { get; }
            public ImmutableArray<UserSearchNotifierResult> SearchNotificactions { get; }
        }
        #endregion
    }
}