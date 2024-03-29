﻿using MemCheck.Application.QueryValidation;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.History;

public sealed class GetCardDiff : RequestRunner<GetCardDiff.Request, GetCardDiff.Result>
{
    #region Private methods
    #endregion
    public GetCardDiff(CallContext callContext) : base(callContext)
    {
    }
    protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
    {
        var current = await DbContext.Cards
            .Include(c => c.CardLanguage)
            .Include(c => c.TagsInCards)
            .ThenInclude(t => t.Tag)
            .SingleAsync(c => c.Id == request.CurrentCardId);
        var original = await DbContext.CardPreviousVersions
            .Include(c => c.CardLanguage)
            .Include(c => c.Tags)
            .ThenInclude(t => t.Tag)
            .SingleAsync(c => c.Id == request.OriginalVersionId);

        var result = new Result(current.VersionCreator.GetUserName(), original.VersionCreator.GetUserName(), current.VersionUtcDate, original.VersionUtcDate, current.VersionDescription, original.VersionDescription);
        if (current.FrontSide != original.FrontSide)
            result = result with { FrontSide = new(current.FrontSide, original.FrontSide) };
        if (current.BackSide != original.BackSide)
            result = result with { BackSide = new(current.BackSide, original.BackSide) };
        if (current.AdditionalInfo != original.AdditionalInfo)
            result = result with { AdditionalInfo = new(current.AdditionalInfo, original.AdditionalInfo) };
        if (current.References != original.References)
            result = result with { References = new(current.References, original.References) };
        if (current.CardLanguage != original.CardLanguage)
            result = result with { Language = new(current.CardLanguage.Name, original.CardLanguage.Name) };
        if (!Enumerable.SequenceEqual(current.TagsInCards.Select(t => t.Tag.Name).OrderBy(tagName => tagName), original.Tags.Select(t => t.Tag.Name).OrderBy(tagName => tagName)))
        {
            var currentTags = string.Join(",", current.TagsInCards.Select(t => t.Tag.Name).OrderBy(tagName => tagName));
            var originalTags = string.Join(",", original.Tags.Select(t => t.Tag.Name).OrderBy(tagName => tagName));
            result = result with { Tags = new(currentTags, originalTags) };
        }
        if (!CardVisibilityHelper.CardsHaveSameUsersWithView(current.UsersWithView, original.UsersWithView))
        {
            var currentUsers = string.Join(",", current.UsersWithView.Select(u => u.User.UserName).OrderBy(userName => userName));
            var originalUserIds = original.UsersWithView.Select(u => u.AllowedUserId).ToHashSet();
            var originalUserNames = DbContext.Users.Where(u => originalUserIds.Contains(u.Id)).Select(u => u.UserName);
            var originalUsers = string.Join(",", originalUserNames.OrderBy(userName => userName));
            result = result with { UsersWithView = new(currentUsers, originalUsers) };
        }
        return new ResultWithMetrologyProperties<Result>(result, ("CurrentCardId", request.CurrentCardId.ToString()), ("OriginalVersionId", request.OriginalVersionId.ToString()));
    }
    #region Request and result types
    public sealed record Request(Guid UserId, Guid CurrentCardId, Guid OriginalVersionId) : IRequest
    {
        public async Task CheckValidityAsync(CallContext callContext)
        {
            if (QueryValidationHelper.IsReservedGuid(UserId))
                throw new InvalidOperationException("Invalid user ID");
            var user = await callContext.DbContext.Users.SingleAsync(u => u.Id == UserId);

            var currentCard = await callContext.DbContext.Cards.Include(v => v.UsersWithView).SingleAsync(v => v.Id == CurrentCardId);
            if (!CardVisibilityHelper.CardIsVisibleToUser(UserId, currentCard))
                throw new InvalidOperationException("Current not visible to user");

            var originalCard = await callContext.DbContext.CardPreviousVersions.Include(v => v.UsersWithView).SingleAsync(v => v.Id == OriginalVersionId);
            if (!CardVisibilityHelper.CardIsVisibleToUser(UserId, originalCard))
                throw new InvalidOperationException("Original not visible to user");
        }
    }
    public sealed record Result
    {
        //Properties are null when a field has not changed
        public Result(string currentVersionCreator, string originalVersionCreator, DateTime currentVersionUtcDate, DateTime originalVersionUtcDate, string currentVersionDescription, string originalVersionDescription)
        {
            CurrentVersionCreator = currentVersionCreator;
            OriginalVersionCreator = originalVersionCreator;
            CurrentVersionUtcDate = currentVersionUtcDate;
            OriginalVersionUtcDate = originalVersionUtcDate;
            CurrentVersionDescription = currentVersionDescription;
            OriginalVersionDescription = originalVersionDescription;
        }
        public string CurrentVersionCreator { get; }
        public string OriginalVersionCreator { get; }
        public DateTime CurrentVersionUtcDate { get; }
        public DateTime OriginalVersionUtcDate { get; }
        public string CurrentVersionDescription { get; }
        public string OriginalVersionDescription { get; }
        public (string currentLanguage, string originalLanguage)? Language { get; init; }
        public (string currentFrontSide, string originalFrontSide)? FrontSide { get; set; }
        public (string currentBackSide, string originalBackSide)? BackSide { get; set; }
        public (string currentAdditionalInfo, string originalAdditionalInfo)? AdditionalInfo { get; set; }
        public (string currentReferences, string originalReferences)? References { get; set; }
        public (string currentTags, string originalTags)? Tags { get; set; }
        public (string currentUsersWithView, string originalUsersWithView)? UsersWithView { get; set; }
    }
    #endregion
}
