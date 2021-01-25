using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Loading
{
    internal sealed class CardRatings
    {
        #region Fields
        private readonly ImmutableDictionary<Guid, double> averageRatings;
        private readonly ImmutableDictionary<Guid, int> userRatings;
        private readonly ImmutableDictionary<Guid, int> countOfUserRatings;
        #endregion
        #region Private methods
        private CardRatings(ImmutableDictionary<Guid, double> averageRatings, ImmutableDictionary<Guid, int> userRatings, ImmutableDictionary<Guid, int> countOfUserRatings, ImmutableHashSet<Guid> cardsWithoutEval)
        {
            this.averageRatings = averageRatings;
            this.userRatings = userRatings;
            this.countOfUserRatings = countOfUserRatings;
            CardsWithoutEval = cardsWithoutEval;
        }
        #endregion

        public double Average(Guid cardId)
        {
            return averageRatings.ContainsKey(cardId) ? averageRatings[cardId] : 0;
        }
        public int User(Guid cardId)
        {
            return userRatings.ContainsKey(cardId) ? userRatings[cardId] : 0;
        }
        public int Count(Guid cardId)
        {
            return countOfUserRatings.ContainsKey(cardId) ? countOfUserRatings[cardId] : 0;
        }
        public ImmutableHashSet<Guid> CardsWithoutEval { get; }
        public ImmutableHashSet<Guid> CardsWithAverageRatingAtLeast(int r)
        {
            return averageRatings.Where(IdAndRating => IdAndRating.Value >= r).Select(IdAndRating => IdAndRating.Key).ToImmutableHashSet();
        }
        public ImmutableHashSet<Guid> CardsWithAverageRatingAtMost(int r)
        {
            return averageRatings.Where(IdAndRating => IdAndRating.Value <= r).Select(IdAndRating => IdAndRating.Key).ToImmutableHashSet();
        }

        public static async Task<CardRatings> LoadAsync(MemCheckDbContext dbContext, Guid userId, IEnumerable<Guid> cardIds)
        {
            //This class is internal, so no input validity checking

            var allCardsAverageRatings = await dbContext.UserCardRatings
                .AsNoTracking()
                .GroupBy(userRating => userRating.CardId)
                .Select(grouping => new { cardId = grouping.Key, averageRating = grouping.Average(grouping => grouping.Rating) })
                .ToListAsync(); //Filtering in memory, not on DB, is major for perf
            var selectedCardsActualRatings = allCardsAverageRatings.Where(r => cardIds.Contains(r.cardId)).ToImmutableDictionary(average => average.cardId, average => average.averageRating);

            var allCardsRatingCounts = await dbContext.UserCardRatings
                .AsNoTracking()
                .GroupBy(userRating => userRating.CardId)
                .Select(grouping => new { cardId = grouping.Key, count = grouping.Count() })
                .ToListAsync(); //Filtering in memory, not on DB, is major for perf
            var selectedCardsRatingCounts = allCardsRatingCounts.Where(r => cardIds.Contains(r.cardId)).ToImmutableDictionary(item => item.cardId, item => item.count);

            var allRatingsOfUser = await dbContext.UserCardRatings
                .AsNoTracking()
                .Where(rating => rating.UserId == userId)
                .Select(rating => new { cardId = rating.CardId, rating = rating.Rating })
                .ToListAsync(); //Filtering in memory, not on DB, is major for perf
            var selectedCardsRatingsOfUser = allRatingsOfUser.Where(r => cardIds.Contains(r.cardId)).ToImmutableDictionary(item => item.cardId, item => item.rating);

            var cardsWithoutEval = cardIds.Except(selectedCardsActualRatings.Keys);

            return new CardRatings(selectedCardsActualRatings, selectedCardsRatingsOfUser, selectedCardsRatingCounts, cardsWithoutEval.ToImmutableHashSet());
        }
    }
}