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
        private readonly MemCheckDbContext dbContext;
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
        public UpdateImageMetadata(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task RunAsync(Request request, ILocalized localizer, DateTime? nowUtc = null)
        {
            await request.CheckValidityAsync(localizer, dbContext);
            var image = await dbContext.Images.Include(img => img.Owner).Include(img => img.PreviousVersion).SingleAsync(img => img.Id == request.ImageId);
            var user = await dbContext.Users.SingleAsync(u => u.Id == request.UserId);

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

            dbContext.ImagePreviousVersions.Add(versionFromCurrentImage);

            image.Owner = user;
            image.Name = request.Name;
            image.Description = request.Description;
            image.Source = request.Source;
            image.LastChangeUtcDate = nowUtc ?? DateTime.UtcNow;
            image.VersionType = ImageVersionType.Changes;
            image.VersionDescription = request.VersionDescription;
            image.PreviousVersion = versionFromCurrentImage;

            await dbContext.SaveChangesAsync();
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

                var imageDataBeforeUpdate = await dbContext.Images.Select(img => new { nameBeforeUpdate = img.Name, sourceBeforeUpdate = img.Source, descriptionBeforeUpdate = img.Description }).SingleAsync();

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
