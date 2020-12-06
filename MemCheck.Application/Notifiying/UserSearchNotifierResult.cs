using System.Collections.Immutable;
using System;

namespace MemCheck.Application.Notifying
{
    public sealed record UserSearchNotifierResult
    {
        public UserSearchNotifierResult(int totalNewlyFoundCardCount, int totalNotFoundAnymoreCardCount, ImmutableArray<Guid> newFoundCards, ImmutableArray<Guid> cardsNotFoundAnymore)
        {
            TotalNewlyFoundCardCount = totalNewlyFoundCardCount;
            TotalNotFoundAnymoreCardCount = totalNotFoundAnymoreCardCount;
            NewlyFoundCards = newFoundCards;
            CardsNotFoundAnymore = cardsNotFoundAnymore;
        }
        public int TotalNewlyFoundCardCount { get; init; }
        public int TotalNotFoundAnymoreCardCount { get; init; }
        public ImmutableArray<Guid> NewlyFoundCards { get; init; }
        public ImmutableArray<Guid> CardsNotFoundAnymore { get; init; }
    }
}
