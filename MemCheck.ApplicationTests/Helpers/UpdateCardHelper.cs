using MemCheck.Application.Cards;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Helpers;

public static class UpdateCardHelper
{
    public static UpdateCard.Request RequestForTagChange(Card card, IEnumerable<Guid> tagIds, string? versionDescription = null)
    {
        return new UpdateCard.Request(
            card.Id,
            card.VersionCreator.Id,
            card.FrontSide,
            card.BackSide,
            card.AdditionalInfo,
            card.References,
            card.CardLanguage.Id,
            tagIds,
            card.UsersWithView.Select(uwv => uwv.UserId),
            versionDescription ?? RandomHelper.String()
            );
    }
    public static UpdateCard.Request RequestForVisibilityChange(Card card, IEnumerable<Guid> userWithViewIds, Guid? versionCreator = null, string? versionDescription = null)
    {
        return new UpdateCard.Request(
            card.Id,
             versionCreator ?? card.VersionCreator.Id,
            card.FrontSide,
            card.BackSide,
            card.AdditionalInfo,
            card.References,
            card.CardLanguage.Id,
            card.TagsInCards.Select(t => t.TagId),
            userWithViewIds,
            versionDescription ?? RandomHelper.String()
            );
    }
    public static UpdateCard.Request RequestForFrontSideChange(Card card, string frontSide, Guid? versionCreator = null, string? versionDescription = null)
    {
        return new UpdateCard.Request(
            card.Id,
            versionCreator ?? card.VersionCreator.Id,
            frontSide,
            card.BackSide,
            card.AdditionalInfo,
            card.References,
            card.CardLanguage.Id,
            card.TagsInCards.Select(t => t.TagId),
            card.UsersWithView.Select(uwv => uwv.UserId),
            versionDescription ?? RandomHelper.String()
            );
    }
    public static UpdateCard.Request RequestForBackSideChange(Card card, string backSide, Guid? versionCreator = null, string? versionDescription = null)
    {
        return new UpdateCard.Request(
            card.Id,
            versionCreator ?? card.VersionCreator.Id,
            card.FrontSide,
            backSide,
            card.AdditionalInfo,
            card.References,
            card.CardLanguage.Id,
            card.TagsInCards.Select(t => t.TagId),
            card.UsersWithView.Select(uwv => uwv.UserId),
            versionDescription ?? RandomHelper.String()
            );
    }
    public static UpdateCard.Request RequestForAdditionalInfoChange(Card card, string additionalInfo, Guid? versionCreator = null, string? versionDescription = null)
    {
        return new UpdateCard.Request(
            card.Id,
            versionCreator ?? card.VersionCreator.Id,
            card.FrontSide,
            card.BackSide,
            additionalInfo,
            card.References,
            card.CardLanguage.Id,
            card.TagsInCards.Select(t => t.TagId),
            card.UsersWithView.Select(uwv => uwv.UserId),
            versionDescription ?? RandomHelper.String()
            );
    }
    public static UpdateCard.Request RequestForReferencesChange(Card card, string references, Guid? versionCreator = null, string? versionDescription = null)
    {
        return new UpdateCard.Request(
            card.Id,
            versionCreator ?? card.VersionCreator.Id,
            card.FrontSide,
            card.BackSide,
            card.AdditionalInfo,
            references,
            card.CardLanguage.Id,
            card.TagsInCards.Select(t => t.TagId),
            card.UsersWithView.Select(uwv => uwv.UserId),
            versionDescription ?? RandomHelper.String()
            );
    }
    public static UpdateCard.Request RequestForLanguageChange(Card card, Guid newLanguageId, Guid? versionCreator = null, string? versionDescription = null)
    {
        return new UpdateCard.Request(
            card.Id,
            versionCreator ?? card.VersionCreator.Id,
            card.FrontSide,
            card.BackSide,
            card.AdditionalInfo,
            card.References,
            newLanguageId,
            card.TagsInCards.Select(t => t.TagId),
            card.UsersWithView.Select(uwv => uwv.UserId),
            versionDescription ?? RandomHelper.String()
            );
    }
    public static async Task RunAsync(DbContextOptions<MemCheckDbContext> db, UpdateCard.Request request)
    {
        using var dbContext = new MemCheckDbContext(db);
        await new UpdateCard(dbContext.AsCallContext()).RunAsync(request);
    }
}
