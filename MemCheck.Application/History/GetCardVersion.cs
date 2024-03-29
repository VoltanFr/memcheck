﻿using MemCheck.Application.QueryValidation;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.History;

public sealed class GetCardVersion : RequestRunner<GetCardVersion.Request, GetCardVersion.Result>
{
    public GetCardVersion(CallContext callContext) : base(callContext)
    {
    }
    protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
    {
        var version = await DbContext.CardPreviousVersions
            .Include(card => card.VersionCreator)
            .Include(card => card.CardLanguage)
            .Include(card => card.Tags)
            .ThenInclude(tagInCard => tagInCard.Tag)
            .Include(card => card.UsersWithView)
            .Where(card => card.Id == request.VersionId)
            .AsSingleQuery()
            .SingleOrDefaultAsync();

        if (version == null)
            throw new RequestInputException($"Card version not found: '{request.VersionId}'");

        var userWithViewNames = version.UsersWithView.Select(userWithView => DbContext.Users.Single(u => u.Id == userWithView.AllowedUserId).GetUserName());
        var tagNames = version.Tags.Select(t => t.Tag.Name);

        var result = new Result(
            version.FrontSide,
            version.BackSide,
            version.AdditionalInfo,
            version.References,
            version.CardLanguage.Id,
            version.CardLanguage.Name,
            tagNames,
            userWithViewNames,
            version.VersionUtcDate,
            version.VersionDescription,
            version.VersionCreator.GetUserName()
            );
        return new ResultWithMetrologyProperties<Result>(result, ("VersionId", request.VersionId.ToString()));
    }
    #region Request & Result types
    public sealed record Request(Guid UserId, Guid VersionId) : IRequest
    {
        public async Task CheckValidityAsync(CallContext callContext)
        {
            if (QueryValidationHelper.IsReservedGuid(UserId))
                throw new InvalidOperationException("Invalid user ID");
            var user = await callContext.DbContext.Users.SingleAsync(u => u.Id == UserId);

            var cardVersion = await callContext.DbContext.CardPreviousVersions.Include(v => v.UsersWithView).SingleAsync(v => v.Id == VersionId);
            if (!CardVisibilityHelper.CardIsVisibleToUser(UserId, cardVersion))
                throw new InvalidOperationException("Original not visible to user");
        }
    }
    public sealed record Result(string FrontSide, string BackSide, string AdditionalInfo, string References, Guid LanguageId, string LanguageName, IEnumerable<string> Tags, IEnumerable<string> UsersWithVisibility, DateTime VersionUtcDate, string VersionDescription, string CreatorName);
    #endregion
}

