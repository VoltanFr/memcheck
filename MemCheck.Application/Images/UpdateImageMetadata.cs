using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Images
{
    public sealed class UpdateImageMetadata
    {
        #region Fields
        private readonly CallContext callContext;
        #endregion
        #region Private methods
        private static ImagePreviousVersionType ImagePreviousVersionTypeFromImage(Domain.Image i)
        {
            return i.VersionType switch
            {
                ImageVersionType.Creation => ImagePreviousVersionType.Creation,
                ImageVersionType.Changes => ImagePreviousVersionType.Changes,
                _ => throw new NotImplementedException(),
            };
        }
        #endregion
        public UpdateImageMetadata(CallContext callContext)
        {
            this.callContext = callContext;
        }
        public async Task RunAsync(Request request, DateTime? nowUtc = null)
        {
            await request.CheckValidityAsync(callContext.Localized, callContext.DbContext);
            var image = await callContext.DbContext.Images.Include(img => img.Owner).Include(img => img.PreviousVersion).SingleAsync(img => img.Id == request.ImageId);
            var user = await callContext.DbContext.Users.SingleAsync(u => u.Id == request.UserId);

            var versionFromCurrentImage = new ImagePreviousVersion()
            {
                Image = request.ImageId,
                Owner = image.Owner,
                Name = image.Name,
                Description = image.Description,
                Source = image.Source,
                InitialUploadUtcDate = image.InitialUploadUtcDate,
                VersionUtcDate = image.LastChangeUtcDate,
                OriginalContentType = image.OriginalContentType,
                OriginalSize = image.OriginalSize,
                OriginalBlob = image.OriginalBlob,
                VersionType = ImagePreviousVersionTypeFromImage(image),
                VersionDescription = image.VersionDescription,
                PreviousVersion = image.PreviousVersion,
            };

            callContext.DbContext.ImagePreviousVersions.Add(versionFromCurrentImage);

            image.Owner = user;
            image.Name = request.Name;
            image.Description = request.Description;
            image.Source = request.Source;
            image.LastChangeUtcDate = nowUtc ?? DateTime.UtcNow;
            image.VersionType = ImageVersionType.Changes;
            image.VersionDescription = request.VersionDescription;
            image.PreviousVersion = versionFromCurrentImage;

            await callContext.DbContext.SaveChangesAsync();

            callContext.TelemetryClient.TrackEvent("UpdateImageMetadata",
                ("ImageId", request.ImageId.ToString()),
                ("NewName", request.Name),
                ("NewNameLength", request.Name.Length.ToString()),
                ("DescriptionLength", request.Description.Length.ToString()),
                ("SourceFieldLength", request.Source.Length.ToString()),
                ("VersionDescriptionLength", request.VersionDescription.Length.ToString())
                );
        }
        #region Request class
        public sealed class Request
        {
            public Request(Guid imageId, Guid userId, string name, string source, string description, string versionDescription)
            {
                ImageId = imageId;
                UserId = userId;
                VersionDescription = versionDescription;
                Name = name;
                Source = source;
                Description = description;
            }
            public Guid ImageId { get; }
            public Guid UserId { get; }
            public string Name { get; }
            public string Source { get; }
            public string Description { get; }
            public string VersionDescription { get; }
            public async Task CheckValidityAsync(ILocalized localizer, MemCheckDbContext dbContext)
            {
                if (QueryValidationHelper.IsReservedGuid(UserId))
                    throw new InvalidOperationException("Invalid user id");
                if (QueryValidationHelper.IsReservedGuid(ImageId))
                    throw new InvalidOperationException("Invalid image id");

                var imageDataBeforeUpdate = await dbContext.Images
                    .AsNoTracking()
                    .Where(img => img.Id == ImageId)
                    .Select(img => new { nameBeforeUpdate = img.Name, sourceBeforeUpdate = img.Source, descriptionBeforeUpdate = img.Description })
                    .SingleAsync();

                if (imageDataBeforeUpdate.nameBeforeUpdate == Name && imageDataBeforeUpdate.sourceBeforeUpdate == Source && imageDataBeforeUpdate.descriptionBeforeUpdate == Description)
                    throw new RequestInputException(localizer.Get("CanNotUpdateMetadataBecauseSameAsOriginal"));

                if (imageDataBeforeUpdate.nameBeforeUpdate != Name)
                    await QueryValidationHelper.CheckCanCreateImageWithNameAsync(Name, dbContext, localizer);

                QueryValidationHelper.CheckCanCreateImageWithSource(Source, localizer);
                QueryValidationHelper.CheckCanCreateImageWithDescription(Description, localizer);
                QueryValidationHelper.CheckCanCreateImageWithVersionDescription(VersionDescription, localizer);
            }
        }
        #endregion
    }
}
