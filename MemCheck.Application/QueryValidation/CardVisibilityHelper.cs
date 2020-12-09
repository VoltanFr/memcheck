using MemCheck.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

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
    }
}