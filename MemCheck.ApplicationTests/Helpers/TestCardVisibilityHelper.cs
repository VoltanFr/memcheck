using MemCheck.Application.QueryValidation;
using MemCheck.Basics;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace MemCheck.Application.Helpers;

internal static class TestCardVisibilityHelper
{
    public static void CheckUserIsAllowedToViewCard(DbContextOptions<MemCheckDbContext> db, Guid userId, Guid cardId)
    {
        using var dbContext = new MemCheckDbContext(db);
        CheckUserIsAllowedToViewCards(db, userId, cardId.AsArray());
    }
    public static void CheckUserIsAllowedToViewCards(DbContextOptions<MemCheckDbContext> db, Guid userId, IEnumerable<Guid> cardIds)
    {
        using var dbContext = new MemCheckDbContext(db);
        CardVisibilityHelper.CheckUserIsAllowedToViewCards(dbContext, userId, cardIds);
    }
}
