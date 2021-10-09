using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Ratings
{
    public sealed class SetCardRating
    {
        #region Fields
        private readonly CallContext callContext;
        #endregion
        #region Private methods
        private async Task SaveRatingByUserAsync(Request request)
        {
            var existingRatingByUser = await callContext.DbContext.UserCardRatings.Where(rating => rating.UserId == request.UserId && rating.CardId == request.CardId).SingleOrDefaultAsync();

            if (existingRatingByUser == null)
                callContext.DbContext.UserCardRatings.Add(new UserCardRating() { UserId = request.UserId, CardId = request.CardId, Rating = request.Rating });
            else
                existingRatingByUser.Rating = request.Rating;

            await callContext.DbContext.SaveChangesAsync();
        }
        private async Task UpdateCardAsync(Guid cardId)
        {
            var newAverageRatingForThisCard = await callContext.DbContext.UserCardRatings.AsNoTracking().Where(rating => rating.CardId == cardId).Select(rating => rating.Rating).AverageAsync();
            var newRatingCountForThisCard = await callContext.DbContext.UserCardRatings.AsNoTracking().Where(rating => rating.CardId == cardId).CountAsync();

            var card = await callContext.DbContext.Cards.SingleAsync(card => card.Id == cardId);
            card.RatingCount = newRatingCountForThisCard;
            card.AverageRating = newAverageRatingForThisCard;
            await callContext.DbContext.SaveChangesAsync();
        }
        #endregion
        public SetCardRating(CallContext callContext)
        {
            this.callContext = callContext;
        }
        public async Task RunAsync(Request request)
        {
            await request.CheckValidityAsync(callContext.DbContext);
            await SaveRatingByUserAsync(request);
            await UpdateCardAsync(request.CardId);
            callContext.TelemetryClient.TrackEvent("SetCardRating", ("CardId", request.CardId.ToString()), ("Rating", request.Rating.ToString()));
        }
        #region Request class
        public sealed class Request
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
        #endregion
    }
}