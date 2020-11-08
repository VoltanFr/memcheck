using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application
{
    public sealed class GetImageVersions
    {
        #region Fields
        private const string ImageNameFieldName = "ImageName";
        private const string ImageDescriptionFieldName = "ImageDescription";
        private const string ImageSourceFieldName = "ImageSource";
        private readonly MemCheckDbContext dbContext;
        private readonly IStringLocalizer localizer;
        #endregion
        #region Private methods
        #endregion
        public GetImageVersions(MemCheckDbContext dbContext, IStringLocalizer localizer)
        {
            this.dbContext = dbContext;
            this.localizer = localizer;
        }
        public async Task<IEnumerable<ResultImageVersion>> RunAsync(Guid imageId)
        {
            if (QueryValidationHelper.IsReservedGuid(imageId))
                throw new RequestInputException("Invalid image id");
            var images = dbContext.Images.Where(img => img.Id == imageId);
            if (!await images.AnyAsync())
                throw new RequestInputException(localizer["UnknownImage"]);

            var currentVersion = await images.Include(img => img.PreviousVersion)
                .Select(img => new ImageVersionFromDb(
                    img.Id,
                    img.PreviousVersion == null ? (Guid?)null : img.PreviousVersion.Id,
                    img.LastChangeUtcDate,
                    img.Owner,
                    img.Name,
                    img.Description,
                    img.Source,
                    img.VersionDescription)
                ).SingleAsync();

            var allPreviousVersions = dbContext.ImagePreviousVersions.Where(img => img.Image == imageId)
                .Select(img => new ImageVersionFromDb(
                    img.Id,
                    img.PreviousVersion == null ? (Guid?)null : img.PreviousVersion.Id,
                    img.VersionUtcDate,
                    img.Owner,
                    img.Name,
                    img.Description,
                    img.Source,
                    img.VersionDescription)
                );

            var versionDico = allPreviousVersions.ToImmutableDictionary(ver => ver.Id, ver => ver);

            var result = new List<ResultImageVersion>();
            var iterationVersion = currentVersion.PreviousVersion == null ? null : versionDico[currentVersion.PreviousVersion.Value];
            result.Add(new ResultImageVersion(currentVersion, iterationVersion));

            while (iterationVersion != null)
            {
                var previousVersion = iterationVersion.PreviousVersion == null ? null : versionDico[iterationVersion.PreviousVersion.Value];
                result.Add(new ResultImageVersion(iterationVersion, previousVersion));
                iterationVersion = previousVersion;
            }
            return result;
        }
        public sealed class ImageVersionFromDb
        {
            public ImageVersionFromDb(Guid id, Guid? previousVersion, DateTime versionUtcDate, MemCheckUser author, string name, string description, string source, string versionDescription)
            {
                Id = id;
                PreviousVersion = previousVersion;
                VersionUtcDate = versionUtcDate;
                Author = author;
                Name = name;
                Description = description;
                Source = source;
                VersionDescription = versionDescription;
            }
            public Guid Id { get; }
            public Guid? PreviousVersion { get; }
            public DateTime VersionUtcDate { get; }
            public MemCheckUser Author { get; }
            public string Name { get; }
            public string Description { get; }
            public string Source { get; }
            public string VersionDescription { get; }
        }
        public sealed class ResultImageVersion
        {
            internal ResultImageVersion(ImageVersionFromDb dbVersion, ImageVersionFromDb? preceedingVersion)
            {
                VersionUtcDate = dbVersion.VersionUtcDate;
                Author = dbVersion.Author.UserName;
                VersionDescription = dbVersion.VersionDescription;
                var changedFieldNames = new List<string>();
                if (preceedingVersion == null)
                    changedFieldNames.AddRange(new[] { ImageNameFieldName, ImageDescriptionFieldName, ImageSourceFieldName });
                else
                {
                    if (dbVersion.Name != preceedingVersion.Name) changedFieldNames.Add(ImageNameFieldName);
                    if (dbVersion.Description != preceedingVersion.Description) changedFieldNames.Add(ImageDescriptionFieldName);
                    if (dbVersion.Source != preceedingVersion.Source) changedFieldNames.Add(ImageSourceFieldName);
                }
                ChangedFieldNames = changedFieldNames;
            }
            public DateTime VersionUtcDate { get; }
            public string Author { get; }
            public string VersionDescription { get; }
            public IEnumerable<string> ChangedFieldNames { get; }
        }
    }
}
