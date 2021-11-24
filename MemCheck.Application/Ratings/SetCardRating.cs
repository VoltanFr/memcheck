using MemCheck.Application.QueryValidation;
using MemCheck.Basics;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Ratings
{
    public sealed class SetCardRating : RequestRunner<SetCardRating.Request, SetCardRating.Result>
    {
        #region Private methods
        private async Task<int> SaveRatingByUserAsync(Request request)
        {
            int attempts = 0;
            while (true)
            {
                var existingRatingByUser = await DbContext.UserCardRatings.Where(rating => rating.UserId == request.UserId && rating.CardId == request.CardId).SingleOrDefaultAsync();

                if (existingRatingByUser == null)
                {
                    try
                    {
                        DbContext.UserCardRatings.Add(new UserCardRating() { UserId = request.UserId, CardId = request.CardId, Rating = request.Rating });
                        await DbContext.SaveChangesAsync();
                        return 0;
                    }
                    catch (SqlException e)
                    {
                        if (attempts < 5 && e.Message.Contains("Violation of PRIMARY KEY constraint"))
                        {
                            //Production metrics (Azure Application Insights) show that we are sometimes in this case. My analysis is that this happens because of the JavaScript retries (in Learn.js/handlePendingRatingOperations)
                            attempts++;
                            await Task.Delay(TimeSpan.FromMilliseconds(Randomizer.Next(50, 1000)));
                        }
                        else
                            throw;
                    }
                }
                else
                {
                    if (existingRatingByUser.Rating == request.Rating)
                        return existingRatingByUser.Rating;

                    var result = existingRatingByUser.Rating;
                    existingRatingByUser.Rating = request.Rating;
                    await DbContext.SaveChangesAsync();
                    return result;
                }
            }
        }
        private async Task UpdateCardAsync(Guid cardId)
        {
            var newAverageRatingForThisCard = await DbContext.UserCardRatings.AsNoTracking().Where(rating => rating.CardId == cardId).Select(rating => rating.Rating).AverageAsync();
            var newRatingCountForThisCard = await DbContext.UserCardRatings.AsNoTracking().Where(rating => rating.CardId == cardId).CountAsync();

            var card = await DbContext.Cards.SingleAsync(card => card.Id == cardId);
            card.RatingCount = newRatingCountForThisCard;
            card.AverageRating = newAverageRatingForThisCard;
            await DbContext.SaveChangesAsync();
        }
        #endregion
        public SetCardRating(CallContext callContext) : base(callContext)
        {
        }
        protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
        {
            var previousValue = await SaveRatingByUserAsync(request);
            if (previousValue != request.Rating)
                await UpdateCardAsync(request.CardId);
            return new ResultWithMetrologyProperties<Result>(new Result(), ("CardId", request.CardId.ToString()), ("Rating", request.Rating.ToString()), ("PreviousValue", previousValue.ToString()));
        }
        #region Request class
        public sealed class Request : IRequest
        {
            public Request(Guid userId, Guid cardId, int rating)
            {
                UserId = userId;
                CardId = cardId;
                Rating = rating;
            }
            public Guid UserId { get; }
            public Guid CardId { get; }
            public int Rating { get; }
            public async Task CheckValidityAsync(MemCheckDbContext dbContext)
            {
                await QueryValidationHelper.CheckUserExistsAsync(dbContext, UserId);
                await QueryValidationHelper.CheckCardExistsAsync(dbContext, CardId);
                if (Rating < 1 || Rating > 5)
                    throw new RequestInputException($"Invalid rating: {Rating}");
            }
        }
        public sealed class Result
        {
        }
        #endregion
    }
}
