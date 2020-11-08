using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.Extensions.Localization;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace MemCheck.Application
{
    public sealed class DeleteImage
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        private readonly IStringLocalizer localizer;
        #endregion
        #region Private methods
        private ImagePreviousVersionType ImagePreviousVersionTypeFromImage(Image i)
        {
            switch (i.VersionType)
            {
                case ImageVersionType.Creation:
                    return ImagePreviousVersionType.Creation;
                case ImageVersionType.Changes:
                    return ImagePreviousVersionType.Changes;
                default:
                    throw new NotImplementedException();
            }
        }
        #endregion
        public DeleteImage(MemCheckDbContext dbContext, IStringLocalizer localizer)
        {
            this.dbContext = dbContext;
            this.localizer = localizer;
        }
        public async Task<string> RunAsync(Request request)
        {
            await request.CheckValidityAsync(localizer, dbContext);

            var image = await dbContext.Images.Where(img => img.Id == request.ImageId).SingleAsync();

            //For a deletion, we create two previous versions:
            //- one for the last known operation (described in the image)
            //- one for the deletion operation

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

            var deletionVersion = new ImagePreviousVersion()
            {
                Image = request.ImageId,
                Owner = request.User,
                Name = image.Name,
                Description = image.Description,
                Source = image.Source,
                InitialUploadUtcDate = image.InitialUploadUtcDate,
                VersionUtcDate = DateTime.UtcNow,
                OriginalContentType = image.OriginalContentType,
                OriginalSize = image.OriginalSize,
                OriginalBlob = image.OriginalBlob,
                VersionType = ImagePreviousVersionType.Deletion,
                VersionDescription = request.DeletionDescription,
                PreviousVersion = versionFromCurrentImage
            };

            dbContext.ImagePreviousVersions.Add(versionFromCurrentImage);
            dbContext.ImagePreviousVersions.Add(deletionVersion);
            dbContext.Images.Remove(image);

            await dbContext.SaveChangesAsync();

            return image.Name;
        }
        #region Request class
        public sealed class Request
        {
            #region Fields
            private const int minDescriptionLength = 3;
            private const int maxDescriptionLength = 1000;
            #endregion
            public Request(MemCheckUser user, Guid imageId, string deletionDescription)
            {
                User = user;
                ImageId = imageId;
                DeletionDescription = deletionDescription.Trim();
            }

            public MemCheckUser User { get; }
            public Guid ImageId { get; }
            public string DeletionDescription { get; }
            public async Task CheckValidityAsync(IStringLocalizer localizer, MemCheckDbContext dbContext)
            {
                if (DeletionDescription != DeletionDescription.Trim())
                    throw new InvalidOperationException("Invalid name: not trimmed");
                if (DeletionDescription.Length < minDescriptionLength || DeletionDescription.Length > maxDescriptionLength)
                    throw new RequestInputException(localizer["InvalidDeletionDescriptionLength"] + $" {DeletionDescription.Length}" + localizer["MustBeBetween"] + $" {minDescriptionLength} " + localizer["And"] + $" {maxDescriptionLength}");

                if (QueryValidationHelper.IsReservedGuid(User.Id))
                    throw new RequestInputException("InvalidUserId");
                if (QueryValidationHelper.IsReservedGuid(ImageId))
                    throw new RequestInputException("InvalidImageId");

                var images = dbContext.Images.Where(img => img.Id == ImageId);
                if (!await images.AnyAsync())
                    throw new RequestInputException(localizer["UnknownImage"]);
                var cardCounts = images.Select(img => img.Cards.Count());
                var cardCount = await cardCounts.SingleAsync();
                if (cardCount != 0)
                    throw new RequestInputException(localizer["ImageUsedInCardsPart1"] + ' ' + cardCount + ' ' + localizer["ImageUsedInCardsPart2"]);
            }
        }
        #endregion
    }
}