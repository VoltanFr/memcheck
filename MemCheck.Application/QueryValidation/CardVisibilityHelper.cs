using MemCheck.Application.Searching;
using MemCheck.Basics;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.QueryValidation;

internal static class CardVisibilityHelper
{
    public const string ExceptionMesg_UserNotAllowedToViewCard = "User not allowed to view card";
    public static bool CardIsVisibleToUser(Guid userId, SearchCards.ResultCard card)
    {
        return CardIsVisibleToUser(userId, card.VisibleTo.Select(uwv => uwv.UserId));
    }
    public static bool CardIsVisibleToUser(Guid userId, Card card)
    {
        return CardIsVisibleToUser(userId, card.UsersWithView.Select(uwv => uwv.UserId));
    }
    public static bool CardIsVisibleToUser(Guid userId, CardPreviousVersion card)
    {
        return CardIsVisibleToUser(userId, card.UsersWithView.Select(uwv => uwv.AllowedUserId));
    }
    public static bool CardIsVisibleToUser(Guid userId, IEnumerable<Guid> usersWithView)
    {
        return !usersWithView.Any() || usersWithView.Any(userWithView => userWithView == userId);
    }
    public static bool CardIsPublic(IEnumerable<Guid> usersWithView)
    {
        return !usersWithView.Any();
    }
    public static bool CardIsPublic(IEnumerable<UserWithViewOnCard> usersWithView)
    {
        return !usersWithView.Any();
    }
    public static async Task CheckCardsArePrivateAsync(MemCheckDbContext dbContext, IEnumerable<Guid> cardIds, Guid userId)
    {
        await Task.CompletedTask;
        if (await dbContext.Cards.AsNoTracking().AnyAsync(card => cardIds.Contains(card.Id) && (card.UsersWithView.Count() != 1 || card.UsersWithView.Single().UserId != userId)))
            throw new RequestInputException("At least one non-private card");
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Too complicated")]
    public static bool CardIsPrivateToSingleUser(Guid userId, IEnumerable<Guid> usersWithView)
    {
        if (QueryValidationHelper.IsReservedGuid(userId))
            throw new InvalidOperationException("Invalid user id");
        if (usersWithView.Any(uwv => QueryValidationHelper.IsReservedGuid(uwv)))
            throw new InvalidOperationException("Invalid user with view id");
        return usersWithView.Count() == 1 && usersWithView.Single(userWithView => userWithView == userId) == userId;
    }
    public static bool CardIsPrivateToSingleUser(Guid userId, IEnumerable<UserWithViewOnCard> usersWithView)
    {
        return CardIsPrivateToSingleUser(userId, usersWithView.Select(uwv => uwv.UserId));
    }
    public static void CheckUserIsAllowedToViewCard(MemCheckDbContext dbContext, Guid userId, Guid cardId)
    {
        CheckUserIsAllowedToViewCards(dbContext, userId, cardId.AsArray());
    }
    public static void CheckUserIsAllowedToViewCards(MemCheckDbContext dbContext, Guid userId, IEnumerable<Guid> cardIds)
    {
        var cards = dbContext.Cards
            .AsNoTracking()
            .Include(card => card.UsersWithView)
            .Include(card => card.VersionCreator)
            .Where(card => cardIds.Contains(card.Id))
            .ToImmutableArray();
        var cardIdsFromDb = cards.Select(card => card.Id).ToImmutableHashSet();
        if (cardIds.Any(cardId => !cardIdsFromDb.Contains(cardId)))
            throw new InvalidOperationException(QueryValidationHelper.ExceptionMesg_CardDoesNotExist);
        if (cards.Any(card => !CardIsVisibleToUser(userId, card)))
            throw new InvalidOperationException(ExceptionMesg_UserNotAllowedToViewCard);
    }
    public static bool CardsHaveSameUsersWithView(IEnumerable<UserWithViewOnCard> cardAllowedUsers, IEnumerable<UserWithViewOnCardPreviousVersion> cardPreviousVersionAllowedUsers)
    {
        return ComparisonHelper.SameSetOfGuid(cardAllowedUsers.Select(u => u.UserId), cardPreviousVersionAllowedUsers.Select(u => u.AllowedUserId));
    }
}
