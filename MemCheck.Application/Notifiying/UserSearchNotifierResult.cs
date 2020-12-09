using System.Collections.Immutable;
using System;
using System.Collections.Generic;

namespace MemCheck.Application.Notifying
{
    public sealed record UserSearchNotifierResult
    {
        public UserSearchNotifierResult(string subscriptionName, int totalNewlyFoundCardCount, IEnumerable<CardVersion> newlyFoundCards, int countOfCardsNotFoundAnymore_StillExists_UserAllowedToView, IEnumerable<CardVersion> cardsNotFoundAnymore_StillExists_UserAllowedToView, int countOfCardsNotFoundAnymore_Deleted_UserAllowedToView, IEnumerable<CardDeletion> cardsNotFoundAnymore_Deleted_UserAllowedToView, int countOfCardsNotFoundAnymore_StillExists_UserNotAllowedToView, int countOfCardsNotFoundAnymore_Deleted_UserNotAllowedToView)
        {
            SubscriptionName = subscriptionName;
            TotalNewlyFoundCardCount = totalNewlyFoundCardCount;
            NewlyFoundCards = newlyFoundCards.ToImmutableArray();
            CountOfCardsNotFoundAnymore_StillExists_UserAllowedToView = countOfCardsNotFoundAnymore_StillExists_UserAllowedToView;
            CardsNotFoundAnymore_StillExists_UserAllowedToView = cardsNotFoundAnymore_StillExists_UserAllowedToView.ToImmutableArray();
            CountOfCardsNotFoundAnymore_Deleted_UserAllowedToView = countOfCardsNotFoundAnymore_Deleted_UserAllowedToView;
            CardsNotFoundAnymore_Deleted_UserAllowedToView = cardsNotFoundAnymore_Deleted_UserAllowedToView.ToImmutableArray();
            CountOfCardsNotFoundAnymore_StillExists_UserNotAllowedToView = countOfCardsNotFoundAnymore_StillExists_UserNotAllowedToView;
            CountOfCardsNotFoundAnymore_Deleted_UserNotAllowedToView = countOfCardsNotFoundAnymore_Deleted_UserNotAllowedToView;
        }

        public string SubscriptionName { get; init; }

        public int TotalNewlyFoundCardCount { get; init; }
        public ImmutableArray<CardVersion> NewlyFoundCards { get; init; }

        public int CountOfCardsNotFoundAnymore_StillExists_UserAllowedToView { get; init; }
        public ImmutableArray<CardVersion> CardsNotFoundAnymore_StillExists_UserAllowedToView { get; init; }

        public int CountOfCardsNotFoundAnymore_Deleted_UserAllowedToView { get; init; }
        public ImmutableArray<CardDeletion> CardsNotFoundAnymore_Deleted_UserAllowedToView { get; init; }

        public int CountOfCardsNotFoundAnymore_StillExists_UserNotAllowedToView { get; init; }
        public int CountOfCardsNotFoundAnymore_Deleted_UserNotAllowedToView { get; init; }
    }
}
