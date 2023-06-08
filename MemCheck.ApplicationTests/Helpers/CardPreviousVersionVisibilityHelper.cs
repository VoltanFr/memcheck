using System;
using System.Collections.Generic;
using System.Linq;
using MemCheck.Domain;

namespace MemCheck.Application.Helpers;

internal static class CardPreviousVersionVisibilityHelper
{
    public static bool CardIsVisibleToUser(Guid userId, CardPreviousVersion card)
    {
        return CardIsVisibleToUser(userId, card.UsersWithView.Select(userWithView => userWithView.AllowedUserId));
    }
    public static bool CardIsVisibleToUser(Guid userId, IEnumerable<Guid> usersWithView)
    {
        return !usersWithView.Any() || usersWithView.Any(userWithView => userWithView == userId);
    }
}
