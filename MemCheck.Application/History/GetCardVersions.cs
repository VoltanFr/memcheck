using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.History
{
    public sealed class GetCardVersions
    {
        #region Fields
        private const string LanguageName = nameof(LanguageName);
        private const string FrontSide = nameof(FrontSide);
        private const string BackSide = nameof(BackSide);
        private const string AdditionalInfo = nameof(AdditionalInfo);
        private const string Tags = nameof(Tags);
        private const string UsersWithView = nameof(UsersWithView);
        private const string FrontSideImages = nameof(FrontSideImages);
        private const string BackSideImages = nameof(BackSideImages);
        private const string AdditionalInfoImages = nameof(AdditionalInfoImages);
        private readonly CallContext callContext;
        #endregion
        #region Private classes
        private sealed class CardVersionFromDb
        {
            public CardVersionFromDb(Guid id, bool isCurrent, Guid? previousVersion, DateTime versionUtcDate, MemCheckUser versionCreator, string versionDescription, Guid languageId, string frontSide, string backSide, string additionalInfo, IEnumerable<Guid> tagIds, IEnumerable<Guid> userWithViewIds, IEnumerable<Guid> frontSideImages, IEnumerable<Guid> backSideImages, IEnumerable<Guid> additionalInfoImages)
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
                TagIds = tagIds;
                UserWithViewIds = userWithViewIds;
                FrontSideImages = frontSideImages;
                BackSideImages = backSideImages;
                AdditionalInfoImages = additionalInfoImages;
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
            public IEnumerable<Guid> TagIds { get; }
            public IEnumerable<Guid> UserWithViewIds { get; }
            public IEnumerable<Guid> FrontSideImages { get; }
            public IEnumerable<Guid> BackSideImages { get; }
            public IEnumerable<Guid> AdditionalInfoImages { get; }
        }
        private sealed class ResultCardVersion : IResultCardVersion
        {
            public ResultCardVersion(CardVersionFromDb dbVersion, CardVersionFromDb? preceedingVersion)
            {
                VersionId = dbVersion.IsCurrent ? null : dbVersion.Id;
                VersionUtcDate = dbVersion.VersionUtcDate;
                VersionCreator = dbVersion.VersionCreator.UserName;
                VersionDescription = dbVersion.VersionDescription;
                var changedFieldNames = new List<string>();
                if (preceedingVersion == null)
                {
                    changedFieldNames.AddRange(new[] { LanguageName, UsersWithView });
                    if (!string.IsNullOrEmpty(dbVersion.FrontSide)) changedFieldNames.Add(FrontSide);
                    if (!string.IsNullOrEmpty(dbVersion.BackSide)) changedFieldNames.Add(BackSide);
                    if (!string.IsNullOrEmpty(dbVersion.AdditionalInfo)) changedFieldNames.Add(AdditionalInfo);
                    if (dbVersion.TagIds.Any()) changedFieldNames.Add(Tags);
                    if (dbVersion.BackSideImages.Any()) changedFieldNames.Add(BackSideImages);
                    if (dbVersion.AdditionalInfoImages.Any()) changedFieldNames.Add(AdditionalInfoImages);
                }
                else
                {
                    if (dbVersion.LanguageId != preceedingVersion.LanguageId) changedFieldNames.Add(LanguageName);
                    if (dbVersion.FrontSide != preceedingVersion.FrontSide) changedFieldNames.Add(FrontSide);
                    if (dbVersion.BackSide != preceedingVersion.BackSide) changedFieldNames.Add(BackSide);
                    if (dbVersion.AdditionalInfo != preceedingVersion.AdditionalInfo) changedFieldNames.Add(AdditionalInfo);
                    if (!Enumerable.SequenceEqual(dbVersion.TagIds, preceedingVersion.TagIds)) changedFieldNames.Add(Tags);
                    if (!Enumerable.SequenceEqual(dbVersion.UserWithViewIds, preceedingVersion.UserWithViewIds)) changedFieldNames.Add(UsersWithView);
                    if (!Enumerable.SequenceEqual(dbVersion.FrontSideImages, preceedingVersion.FrontSideImages)) changedFieldNames.Add(FrontSideImages);
                    if (!Enumerable.SequenceEqual(dbVersion.BackSideImages, preceedingVersion.BackSideImages)) changedFieldNames.Add(BackSideImages);
                    if (!Enumerable.SequenceEqual(dbVersion.AdditionalInfoImages, preceedingVersion.AdditionalInfoImages)) changedFieldNames.Add(AdditionalInfoImages);
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
        public GetCardVersions(CallContext callContext)
        {
            this.callContext = callContext;
        }
        public async Task<IEnumerable<IResultCardVersion>> RunAsync(Request request)
        {
            await request.CheckValidityAsync(callContext.DbContext);

            var currentVersion = await callContext.DbContext.Cards.Include(card => card.PreviousVersion)
                .Where(card => card.Id == request.CardId)
                .Select(card => new CardVersionFromDb(
                    card.Id,
                    true,
                    card.PreviousVersion == null ? (Guid?)null : card.PreviousVersion.Id,
                    card.VersionUtcDate,
                    card.VersionCreator,
                    card.VersionDescription,
                    card.CardLanguage.Id,
                    card.FrontSide,
                    card.BackSide,
                    card.AdditionalInfo,
                    card.TagsInCards.Select(tag => tag.TagId),
                    card.UsersWithView.Select(user => user.UserId),
                    card.Images.Where(img => img.CardSide == ImageInCard.FrontSide).Select(img => img.ImageId),
                    card.Images.Where(img => img.CardSide == ImageInCard.BackSide).Select(img => img.ImageId),
                    card.Images.Where(img => img.CardSide == ImageInCard.AdditionalInfo).Select(img => img.ImageId)
                    )
                ).SingleAsync();

            var allPreviousVersions = callContext.DbContext.CardPreviousVersions
                .Where(card => card.Card == request.CardId)
                .Select(card => new CardVersionFromDb(
                    card.Id,
                    false,
                    card.PreviousVersion == null ? (Guid?)null : card.PreviousVersion.Id,
                    card.VersionUtcDate,
                    card.VersionCreator,
                    card.VersionDescription,
                    card.CardLanguage.Id,
                    card.FrontSide,
                    card.BackSide,
                    card.AdditionalInfo,
                    card.Tags.Select(tag => tag.TagId),
                    card.UsersWithView.Select(u => u.AllowedUserId),
                    card.Images.Where(img => img.CardSide == 1).Select(img => img.ImageId),
                    card.Images.Where(img => img.CardSide == 2).Select(img => img.ImageId),
                    card.Images.Where(img => img.CardSide == 3).Select(img => img.ImageId)
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

            callContext.TelemetryClient.TrackEvent("GetCardVersions", ("CardId", request.CardId.ToString()), ("ResultCount", result.Count.ToString()));
            return result;
        }
        #region Request and result types
        public sealed record Request(Guid UserId, Guid CardId)
        {
            public async Task CheckValidityAsync(MemCheckDbContext dbContext)
            {
                //We allow viewing the history of a card as soon as the user can access the current version of the card. Of course the differ will refuse to give details to a user not allowed

                QueryValidationHelper.CheckNotReservedGuid(UserId);
                QueryValidationHelper.CheckNotReservedGuid(CardId);

                var user = await dbContext.Users.SingleAsync(u => u.Id == UserId);

                var card = await dbContext.Cards.Include(v => v.UsersWithView).SingleAsync(v => v.Id == CardId);
                if (!CardVisibilityHelper.CardIsVisibleToUser(UserId, card.UsersWithView))
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
}
