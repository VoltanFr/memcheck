﻿using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
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
                throw new InvalidOperationException("Invalid user id");
            if (usersWithView.Any(uwv => QueryValidationHelper.IsReservedGuid(uwv)))
                throw new InvalidOperationException("Invalid user with view id");
            return !usersWithView.Any() || usersWithView.Any(userWithView => userWithView == userId);
        }
        public static void CheckUserIsAllowedToViewCards(MemCheckDbContext dbContext, Guid userId, params Guid[] cardIds)
        {
            var cards = dbContext.Cards
                .AsNoTracking()
                .Include(card => card.UsersWithView)
                .Include(card => card.VersionCreator)
                .Where(card => cardIds.Contains(card.Id));
            foreach (var card in cards)
                if (!CardIsVisibleToUser(userId, card.UsersWithView))
                    throw new InvalidOperationException("User not allowed to view card");
        }
        public static bool CardsHaveSameUsersWithView(IEnumerable<UserWithViewOnCard> cardAllowedUsers, IEnumerable<UserWithViewOnCardPreviousVersion> cardPreviousVersionAllowedUsers)
        {
            return ComparisonHelper.SameSetOfGuid(cardAllowedUsers.Select(u => u.UserId), cardPreviousVersionAllowedUsers.Select(u => u.AllowedUserId));
        }
    }
}