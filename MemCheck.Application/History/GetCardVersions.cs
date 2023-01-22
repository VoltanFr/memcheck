using MemCheck.Application.QueryValidation;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.History;

public sealed class GetCardVersions : RequestRunner<GetCardVersions.Request, IEnumerable<GetCardVersions.IResultCardVersion>>
{
    #region Fields
    public const string LanguageName = nameof(LanguageName);
    public const string FrontSide = nameof(FrontSide);
    public const string BackSide = nameof(BackSide);
    public const string AdditionalInfo = nameof(AdditionalInfo);
    public const string References = nameof(References);
    private const string Tags = nameof(Tags);
    public const string UsersWithView = nameof(UsersWithView);
    #endregion
    #region Private classes
    private sealed class CardVersionFromDb
    {
        public CardVersionFromDb(Guid id, bool isCurrent, Guid? previousVersion, DateTime versionUtcDate, MemCheckUser versionCreator, string versionDescription, Guid languageId, string frontSide, string backSide, string additionalInfo, string references, IEnumerable<Guid> tagIds, IEnumerable<Guid> userWithViewIds)
        {
            Id = id;
            IsCurrent = isCurrent;
            PreviousVersion = previousVersion;
            VersionUtcDate = versionUtcDate;
            VersionCreator = versionCreator;
            VersionDescription = versionDescription;
            LanguageId = languageId;
            FrontSide = frontSide;
            BackSide = backSide;
            AdditionalInfo = additionalInfo;
            References = references;
            TagIds = tagIds;
            UserWithViewIds = userWithViewIds;
        }
        public Guid Id { get; }
        public bool IsCurrent { get; }  //True means that this is the card. False means this is a previous version of the card
        public Guid? PreviousVersion { get; }
        public DateTime VersionUtcDate { get; }
        public MemCheckUser VersionCreator { get; }
        public string VersionDescription { get; }
        public Guid LanguageId { get; }
        public string FrontSide { get; }
        public string BackSide { get; }
        public string AdditionalInfo { get; }
        public string References { get; }
        public IEnumerable<Guid> TagIds { get; }
        public IEnumerable<Guid> UserWithViewIds { get; }
    }
    private sealed class ResultCardVersion : IResultCardVersion
    {
        public ResultCardVersion(CardVersionFromDb dbVersion, CardVersionFromDb? preceedingVersion)
        {
            VersionId = dbVersion.IsCurrent ? null : dbVersion.Id;
            VersionUtcDate = dbVersion.VersionUtcDate;
            VersionCreator = dbVersion.VersionCreator.GetUserName();
            VersionDescription = dbVersion.VersionDescription;
            var changedFieldNames = new List<string>();
            if (preceedingVersion == null)
            {
                changedFieldNames.AddRange(new[] { LanguageName, UsersWithView });
                if (!string.IsNullOrEmpty(dbVersion.FrontSide)) changedFieldNames.Add(FrontSide);
                if (!string.IsNullOrEmpty(dbVersion.BackSide)) changedFieldNames.Add(BackSide);
                if (!string.IsNullOrEmpty(dbVersion.AdditionalInfo)) changedFieldNames.Add(AdditionalInfo);
                if (!string.IsNullOrEmpty(dbVersion.References)) changedFieldNames.Add(References);
                if (dbVersion.TagIds.Any()) changedFieldNames.Add(Tags);
            }
            else
            {
                if (dbVersion.LanguageId != preceedingVersion.LanguageId) changedFieldNames.Add(LanguageName);
                if (dbVersion.FrontSide != preceedingVersion.FrontSide) changedFieldNames.Add(FrontSide);
                if (dbVersion.BackSide != preceedingVersion.BackSide) changedFieldNames.Add(BackSide);
                if (dbVersion.AdditionalInfo != preceedingVersion.AdditionalInfo) changedFieldNames.Add(AdditionalInfo);
                if (dbVersion.References != preceedingVersion.References) changedFieldNames.Add(References);
                if (!Enumerable.SequenceEqual(dbVersion.TagIds, preceedingVersion.TagIds)) changedFieldNames.Add(Tags);
                if (!Enumerable.SequenceEqual(dbVersion.UserWithViewIds, preceedingVersion.UserWithViewIds)) changedFieldNames.Add(UsersWithView);
            }
            ChangedFieldNames = changedFieldNames;
        }
        public Guid? VersionId { get; } //null if this is the current version of the card, ie not a previous version
        public DateTime VersionUtcDate { get; }
        public string VersionCreator { get; }
        public string VersionDescription { get; }
        public IEnumerable<string> ChangedFieldNames { get; }
    }
    #endregion
    public GetCardVersions(CallContext callContext) : base(callContext)
    {
    }
    protected override async Task<ResultWithMetrologyProperties<IEnumerable<IResultCardVersion>>> DoRunAsync(Request request)
    {
        var currentVersion = await DbContext.Cards.Include(card => card.PreviousVersion)
            .Where(card => card.Id == request.CardId)
            .Select(card => new CardVersionFromDb(
                card.Id,
                true,
                card.PreviousVersion == null ? null : card.PreviousVersion.Id,
                card.VersionUtcDate,
                card.VersionCreator,
                card.VersionDescription,
                card.CardLanguage.Id,
                card.FrontSide,
                card.BackSide,
                card.AdditionalInfo,
                card.References,
                card.TagsInCards.Select(tag => tag.TagId),
                card.UsersWithView.Select(user => user.UserId)
                )
            ).SingleAsync();

        var allPreviousVersions = DbContext.CardPreviousVersions
            .Where(card => card.Card == request.CardId)
            .Select(card => new CardVersionFromDb(
                card.Id,
                false,
                card.PreviousVersion == null ? null : card.PreviousVersion.Id,
                card.VersionUtcDate,
                card.VersionCreator,
                card.VersionDescription,
                card.CardLanguage.Id,
                card.FrontSide,
                card.BackSide,
                card.AdditionalInfo,
                card.References,
                card.Tags.Select(tag => tag.TagId),
                card.UsersWithView.Select(u => u.AllowedUserId)
                )
            );

        var versionDico = allPreviousVersions.ToImmutableDictionary(ver => ver.Id, ver => ver);

        var result = new List<ResultCardVersion>();
        var iterationVersion = currentVersion.PreviousVersion == null ? null : versionDico[currentVersion.PreviousVersion.Value];
        result.Add(new ResultCardVersion(currentVersion, iterationVersion));

        while (iterationVersion != null)
        {
            var previousVersion = iterationVersion.PreviousVersion == null ? null : versionDico[iterationVersion.PreviousVersion.Value];
            result.Add(new ResultCardVersion(iterationVersion, previousVersion));
            iterationVersion = previousVersion;
        }

        return new ResultWithMetrologyProperties<IEnumerable<IResultCardVersion>>(result, ("CardId", request.CardId.ToString()), IntMetric("ResultCount", result.Count));
    }
    #region Request and result types
    public sealed record Request(Guid UserId, Guid CardId) : IRequest
    {
        public async Task CheckValidityAsync(CallContext callContext)
        {
            //We allow viewing the history of a card as soon as the user can access the current version of the card. Of course the differ will refuse to give details to a user not allowed

            QueryValidationHelper.CheckNotReservedGuid(UserId);
            QueryValidationHelper.CheckNotReservedGuid(CardId);

            var user = await callContext.DbContext.Users.SingleAsync(u => u.Id == UserId);

            var card = await callContext.DbContext.Cards.Include(v => v.UsersWithView).SingleAsync(v => v.Id == CardId);
            if (!CardVisibilityHelper.CardIsVisibleToUser(UserId, card))
                throw new InvalidOperationException("Current not visible to user");
        }
    }
    public interface IResultCardVersion
    {
        public Guid? VersionId { get; } //null if this is the current version of the card, ie not a previous version
        public DateTime VersionUtcDate { get; }
        public string VersionCreator { get; }
        public string VersionDescription { get; }
        public IEnumerable<string> ChangedFieldNames { get; }
    }
    #endregion
}
