using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.QueryValidation
{
    internal static class CardVisibilityHelper
    {
        public static bool CardIsVisibleToUser(Guid userId, IEnumerable<UserWithViewOnCard> usersWithView)
        {
            return CardIsVisibleToUser(userId, usersWithView.Select(uwv => uwv.UserId));
        }
        public static bool CardIsVisibleToUser(Guid userId, IEnumerable<Guid> usersWithView)
        {
            if (QueryValidationHelper.IsReservedGuid(userId))
                throw new ApplicationException("Invalid user id");
            if (usersWithView.Any(uwv => QueryValidationHelper.IsReservedGuid(uwv)))
                throw new ApplicationException("Invalid user with view id");
            return !usersWithView.Any() || usersWithView.Any(userWithView => userWithView == userId);
        }
        public static async Task CheckUserIsAllowedToViewCardAsync(MemCheckDbContext dbContext, Guid userId, Guid cardId)
        {
            if (!await dbContext.Users.AnyAsync(u => u.Id == userId))
                throw new ApplicationException("Invalid user");
            var card = await dbContext.Cards
                .AsNoTracking()
                .Include(card => card.UsersWithView)
                .Include(card => card.VersionCreator)
                .Where(card => card.Id == cardId)
                .SingleAsync();
            if (!CardIsVisibleToUser(userId, card.UsersWithView))
                throw new ApplicationException("User not allowed to view card");
        }
    }
}