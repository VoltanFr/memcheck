using MemCheck.Application.QueryValidation;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace MemCheck.Application.Ratings;

public sealed class SetCardRating : RequestRunner<SetCardRating.Request, SetCardRating.Result>
{
    #region Private methods
    private async Task<int> SaveRatingByUserAsync(Request request)
    {
        var attempts = 0;
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
                    if (attempts < 5 && e.Message.Contains("Violation of PRIMARY KEY constraint", StringComparison.Ordinal))
                    {
                        //Production metrics (Azure Application Insights) show that we are sometimes in this case. My analysis is that this happens because of the JavaScript retries (in Learn.js/handlePendingRatingOperations)
                        attempts++;
                        var waitTimeBeforeRetry = TimeSpan.FromMilliseconds(RandomNumberGenerator.GetInt32(10, 1000));
                        await Task.Delay(waitTimeBeforeRetry);
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
        return new ResultWithMetrologyProperties<Result>(new Result(), ("CardId", request.CardId.ToString()), IntMetric("Rating", request.Rating), IntMetric("PreviousValue", previousValue));
    }
    #region Request & Result
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
        public async Task CheckValidityAsync(CallContext callContext)
        {
            await QueryValidationHelper.CheckUserExistsAsync(callContext.DbContext, UserId);
            await QueryValidationHelper.CheckCardExistsAsync(callContext.DbContext, CardId);
            if (Rating is < 1 or > 5)
                throw new RequestInputException($"Invalid rating: {Rating}");
        }
    }
    public sealed record Result();
    #endregion
}
