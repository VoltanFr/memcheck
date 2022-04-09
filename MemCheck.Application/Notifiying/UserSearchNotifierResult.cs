using System.Collections.Generic;
using System.Collections.Immutable;

namespace MemCheck.Application.Notifying
{
    public sealed record UserSearchNotifierResult
    {
        public UserSearchNotifierResult(string subscriptionName, int totalNewlyFoundCardCount, IEnumerable<CardVersion> newlyFoundCards, int countOfCardsNotFoundAnymoreStillExistsUserAllowedToView,
            IEnumerable<CardVersion> cardsNotFoundAnymoreStillExistsUserAllowedToView, int countOfCardsNotFoundAnymoreDeletedUserAllowedToView, IEnumerable<CardDeletion> cardsNotFoundAnymoreDeletedUserAllowedToView,
            int countOfCardsNotFoundAnymoreStillExistsUserNotAllowedToView, int countOfCardsNotFoundAnymoreDeletedUserNotAllowedToView)
        {
            SubscriptionName = subscriptionName;
            TotalNewlyFoundCardCount = totalNewlyFoundCardCount;
            NewlyFoundCards = newlyFoundCards.ToImmutableArray();
            CountOfCardsNotFoundAnymoreStillExistsUserAllowedToView = countOfCardsNotFoundAnymoreStillExistsUserAllowedToView;
            CardsNotFoundAnymoreStillExistsUserAllowedToView = cardsNotFoundAnymoreStillExistsUserAllowedToView.ToImmutableArray();
            CountOfCardsNotFoundAnymoreDeletedUserAllowedToView = countOfCardsNotFoundAnymoreDeletedUserAllowedToView;
            CardsNotFoundAnymoreDeletedUserAllowedToView = cardsNotFoundAnymoreDeletedUserAllowedToView.ToImmutableArray();
            CountOfCardsNotFoundAnymoreStillExistsUserNotAllowedToView = countOfCardsNotFoundAnymoreStillExistsUserNotAllowedToView;
            CountOfCardsNotFoundAnymoreDeletedUserNotAllowedToView = countOfCardsNotFoundAnymoreDeletedUserNotAllowedToView;
        }

        public string SubscriptionName { get; init; }

        public int TotalNewlyFoundCardCount { get; init; }
        public ImmutableArray<CardVersion> NewlyFoundCards { get; init; }

        public int CountOfCardsNotFoundAnymoreStillExistsUserAllowedToView { get; init; }
        public ImmutableArray<CardVersion> CardsNotFoundAnymoreStillExistsUserAllowedToView { get; init; }

        public int CountOfCardsNotFoundAnymoreDeletedUserAllowedToView { get; init; }
        public ImmutableArray<CardDeletion> CardsNotFoundAnymoreDeletedUserAllowedToView { get; init; }

        public int CountOfCardsNotFoundAnymoreStillExistsUserNotAllowedToView { get; init; }
        public int CountOfCardsNotFoundAnymoreDeletedUserNotAllowedToView { get; init; }
    }
}
