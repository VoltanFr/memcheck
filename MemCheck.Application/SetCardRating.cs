using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application
{
    public sealed class SetCardRating
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public SetCardRating(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task RunAsync(Request request)
        {
            request.CheckValidity();

            var existing = await dbContext.UserCardRatings.Where(rating => rating.UserId == request.User.Id && rating.CardId == request.CardId).SingleOrDefaultAsync();

            if (existing == null)
                dbContext.UserCardRatings.Add(new UserCardRating() { UserId = request.User.Id, CardId = request.CardId, Rating = request.Rating });
            else
                existing.Rating = request.Rating;

            await dbContext.SaveChangesAsync();
        }
        #region Request class
        public sealed class Request
        {
            public Request(MemCheckUser user, Guid cardId, int rating)
            {
                User = user;
                CardId = cardId;
                Rating = rating;
            }
            public MemCheckUser User { get; }
            public Guid CardId { get; }
            public int Rating { get; }
            public void CheckValidity()
            {
                if (QueryValidationHelper.IsReservedGuid(User.Id))
                    throw new RequestInputException("Invalid user id");
                if (QueryValidationHelper.IsReservedGuid(CardId))
                    throw new RequestInputException("Invalid card id");
                if (Rating < 1 || Rating > 5)
                    throw new RequestInputException($"Invalid rating: {Rating}");
            }
        }
        #endregion
    }
}