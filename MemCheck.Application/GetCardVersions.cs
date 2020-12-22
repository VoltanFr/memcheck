using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application
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
        private readonly MemCheckDbContext dbContext;
        private readonly ILocalized localizer;
        #endregion
        #region Private methods
        #endregion
        public GetCardVersions(MemCheckDbContext dbContext, ILocalized localizer)
        {
            this.dbContext = dbContext;
            this.localizer = localizer;
        }
        public async Task<IEnumerable<ResultCardVersion>> RunAsync(Guid cardId, Guid userId)
        {
            if (QueryValidationHelper.IsReservedGuid(cardId))
                throw new RequestInputException("Invalid card id");
            if (QueryValidationHelper.IsReservedGuid(userId))
                throw new RequestInputException("Invalid user id");
            await QueryValidationHelper.CheckUserIsAllowedToViewCardAsync(dbContext, userId, cardId);

            var cards = dbContext.Cards.Where(card => card.Id == cardId);
            if (!await cards.AnyAsync())
                throw new RequestInputException(localizer.Get("UnknownCard"));

            var currentVersion = await cards.Include(card => card.PreviousVersion)
                .Select(card => new CardVersionFromDb(
                    card.Id,
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

            var allPreviousVersions = dbContext.CardPreviousVersions.Where(card => card.Card == cardId)
                .Select(card => new CardVersionFromDb(
                    card.Id,
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
            return result;
        }
        internal sealed class CardVersionFromDb
        {
            public CardVersionFromDb(Guid id, Guid? previousVersion, DateTime versionUtcDate, MemCheckUser versionCreator, string versionDescription, Guid languageId, string frontSide, string backSide, string additionalInfo, IEnumerable<Guid> tagIds, IEnumerable<Guid> userWithViewIds, IEnumerable<Guid> frontSideImages, IEnumerable<Guid> backSideImages, IEnumerable<Guid> additionalInfoImages)
            {
                Id = id;
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
        public sealed class ResultCardVersion
        {
            internal ResultCardVersion(CardVersionFromDb dbVersion, CardVersionFromDb? preceedingVersion)
            {
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
            public DateTime VersionUtcDate { get; }
            public string VersionCreator { get; }
            public string VersionDescription { get; }
            public IEnumerable<string> ChangedFieldNames { get; }
        }
    }
}