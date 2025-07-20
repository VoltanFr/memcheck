using MemCheck.Domain;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MemCheck.Application.Notifiying;

internal sealed class Notifier : RequestRunner<Notifier.Request, Notifier.NotifierResult>
{
    #region Fields
    private readonly IUserCardSubscriptionCounter userCardSubscriptionCounter;
    private readonly IUserSearchSubscriptionLister userSearchSubscriptionLister;
    private readonly IUserCardVersionsNotifier userCardVersionsNotifier;
    private readonly IUserCardDeletionsNotifier userCardDeletionsNotifier;
    private readonly IUsersToNotifyGetter usersToNotifyGetter;
    private readonly IUserLastNotifDateUpdater userLastNotifDateUpdater;
    private readonly IUserSearchNotifier userSearchNotifier;
    private readonly ICollection<string> performanceIndicators;
    private readonly DateTime? runDate;
    public const int MaxLengthForTextFields = 150;
    public const int MaxCardsToReportPerSearch = 100;
    #endregion
    #region Private methods
    private async Task<UserNotifications> GetUserNotificationsAsync(MemCheckUser user)
    {
        performanceIndicators.Add("Getting count of subscribed cards for user");
        var subscribedCardCount = await userCardSubscriptionCounter.RunAsync(user.Id);
        performanceIndicators.Add("Getting card versions for user");
        var versionsNotifierResult = await userCardVersionsNotifier.RunAsync(user.Id);
        performanceIndicators.Add("Getting deleted cards for user");
        var cardDeletions = await userCardDeletionsNotifier.RunAsync(user.Id);
        performanceIndicators.Add("Getting subscriptions for user");
        var subscribedSearches = await userSearchSubscriptionLister.RunAsync(user.Id);
        performanceIndicators.Add("Creating notifications for user");

        var searchNotifs = new List<UserSearchNotifierResult>();
        foreach (var subscribedSearch in subscribedSearches)
            searchNotifs.Add(await userSearchNotifier.RunAsync(subscribedSearch.Id));

        await userLastNotifDateUpdater.RunAsync(user.Id);

        return new UserNotifications(
            user.GetUserName(),
            user.GetEmail().Address,
            subscribedCardCount,
            versionsNotifierResult.Cards,
            cardDeletions,
            searchNotifs
            );
    }
    #endregion
    public Notifier(CallContext callContext, ICollection<string> performanceIndicators)
        : this(
              callContext,
              new UserCardSubscriptionCounter(callContext, performanceIndicators),
              new UserCardVersionsNotifier(callContext, performanceIndicators),
              new UserCardDeletionsNotifier(callContext, performanceIndicators),
              new UsersToNotifyGetter(callContext, performanceIndicators),
              new UserLastNotifDateUpdater(callContext, performanceIndicators, DateTime.UtcNow),
              new UserSearchSubscriptionLister(callContext, performanceIndicators),
              new UserSearchNotifier(callContext, MaxCardsToReportPerSearch, performanceIndicators),
              performanceIndicators)
    {
    }
    internal Notifier(CallContext callContext, IUserCardSubscriptionCounter userCardSubscriptionCounter, IUserCardVersionsNotifier userCardVersionsNotifier, IUserCardDeletionsNotifier userCardDeletionsNotifier, IUsersToNotifyGetter usersToNotifyGetter, IUserLastNotifDateUpdater userLastNotifDateUpdater, IUserSearchSubscriptionLister userSearchSubscriptionLister, IUserSearchNotifier userSearchNotifier, ICollection<string> performanceIndicators, DateTime? runDate = null)
         : base(callContext)
    {
        this.userCardSubscriptionCounter = userCardSubscriptionCounter;
        this.userCardVersionsNotifier = userCardVersionsNotifier;
        this.userCardDeletionsNotifier = userCardDeletionsNotifier;
        this.usersToNotifyGetter = usersToNotifyGetter;
        this.userLastNotifDateUpdater = userLastNotifDateUpdater;
        this.userSearchSubscriptionLister = userSearchSubscriptionLister;
        this.userSearchNotifier = userSearchNotifier;
        this.performanceIndicators = performanceIndicators;
        this.runDate = runDate;
    }
    protected override async Task<ResultWithMetrologyProperties<NotifierResult>> DoRunAsync(Request request)
    {
        var chrono = Stopwatch.StartNew();
        var now = runDate == null ? DateTime.UtcNow : runDate;
        var users = usersToNotifyGetter.Run(now);
        var userNotifications = new List<UserNotifications>();
        foreach (var user in users)
            userNotifications.Add(await GetUserNotificationsAsync(user));
        performanceIndicators.Add($"Total Notifier execution time: {chrono.Elapsed}");
        var result = userNotifications.ToImmutableArray();
        return new ResultWithMetrologyProperties<NotifierResult>(new NotifierResult(result), IntMetric("ResultCount", result.Length));
    }
    #region Request & Result
    public sealed record Request() : IRequest
    {
        public async Task CheckValidityAsync(CallContext callContext)
        {
            await Task.CompletedTask;
        }
    }

    internal class NotifierResult
    {
        public NotifierResult(ImmutableArray<UserNotifications> userNotifications)
        {
            UserNotifications = userNotifications;
        }
        public ImmutableArray<UserNotifications> UserNotifications { get; }
    }
    internal record UserNotifications
    {
        public UserNotifications(string userName, string userEmail, int subscribedCardCount, ImmutableArray<IUserCardVersionsNotifier.ResultCard> cards, IEnumerable<CardDeletion> deletedCards, IEnumerable<UserSearchNotifierResult> searchNotificactions)
        {
            UserName = userName;
            UserEmail = userEmail;
            SubscribedCardCount = subscribedCardCount;
            Cards = cards;
            DeletedCards = deletedCards.ToImmutableArray();
            SearchNotificactions = searchNotificactions.ToImmutableArray();
        }
        public string UserName { get; }
        public string UserEmail { get; }
        public int SubscribedCardCount { get; }
        public ImmutableArray<IUserCardVersionsNotifier.ResultCard> Cards { get; }
        public ImmutableArray<CardDeletion> DeletedCards { get; }
        public ImmutableArray<UserSearchNotifierResult> SearchNotificactions { get; }
    }
    #endregion
}
