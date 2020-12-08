using System.Collections.Immutable;
using System;

namespace MemCheck.Application.Notifying
{
    public sealed record UserSearchNotifierResult
    {
        public UserSearchNotifierResult(string subscriptionName, int totalNewlyFoundCardCount, int totalNotFoundAnymoreCardCount, ImmutableArray<CardVersion> newFoundCards, ImmutableArray<Guid> cardsNotFoundAnymore)
        {
            TotalNewlyFoundCardCount = totalNewlyFoundCardCount;
            TotalNotFoundAnymoreCardCount = totalNotFoundAnymoreCardCount;
            NewlyFoundCards = newFoundCards;
            CardsNotFoundAnymore = cardsNotFoundAnymore;
            SubscriptionName = subscriptionName;
        }
        public string SubscriptionName { get; init; }
        public int TotalNewlyFoundCardCount { get; init; }
        public int TotalNotFoundAnymoreCardCount { get; init; }
        public ImmutableArray<CardVersion> NewlyFoundCards { get; init; }
        public ImmutableArray<Guid> CardsNotFoundAnymore { get; init; }
    }
}
