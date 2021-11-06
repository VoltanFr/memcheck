using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Images
{
    public sealed class DeleteImage
    {
        #region Fields
        private readonly CallContext callContext;
        #endregion
        #region Private methods
        private static ImagePreviousVersionType ImagePreviousVersionTypeFromImage(Image i)
        {
            return i.VersionType switch
            {
                ImageVersionType.Creation => ImagePreviousVersionType.Creation,
                ImageVersionType.Changes => ImagePreviousVersionType.Changes,
                _ => throw new NotImplementedException(),
            };
        }
        #endregion
        public DeleteImage(CallContext callContext)
        {
            this.callContext = callContext;
        }
        public async Task<string> RunAsync(Request request, ILocalized localizer, DateTime? deletionUtcDate = null)
        {
            await request.CheckValidityAsync(localizer, callContext.DbContext);

            var image = await callContext.DbContext.Images.Where(img => img.Id == request.ImageId).SingleAsync();
            var user = await callContext.DbContext.Users.SingleAsync(u => u.Id == request.UserId);

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
                Owner = user,
                Name = image.Name,
                Description = image.Description,
                Source = image.Source,
                InitialUploadUtcDate = image.InitialUploadUtcDate,
                VersionUtcDate = deletionUtcDate ?? DateTime.UtcNow,
                OriginalContentType = image.OriginalContentType,
                OriginalSize = image.OriginalSize,
                OriginalBlob = image.OriginalBlob,
                VersionType = ImagePreviousVersionType.Deletion,
                VersionDescription = request.DeletionDescription,
                PreviousVersion = versionFromCurrentImage
            };

            callContext.DbContext.ImagePreviousVersions.Add(versionFromCurrentImage);
            callContext.DbContext.ImagePreviousVersions.Add(deletionVersion);
            callContext.DbContext.Images.Remove(image);

            await callContext.DbContext.SaveChangesAsync();

            var result = image.Name;
            callContext.TelemetryClient.TrackEvent("DeleteImage", ("ImageId", request.ImageId.ToString()), ("ImageName", result), ("DeletionDescription", request.DeletionDescription));
            return result;
        }
        #region Request type
        public sealed record Request(Guid UserId, Guid ImageId, string DeletionDescription)
        {
            public const int MinDescriptionLength = 3;
            public const int MaxDescriptionLength = 1000;
            public async Task CheckValidityAsync(ILocalized localizer, MemCheckDbContext dbContext)
            {
                if (DeletionDescription != DeletionDescription.Trim())
                    throw new InvalidOperationException("Invalid name: not trimmed");
                if (DeletionDescription.Length < MinDescriptionLength || DeletionDescription.Length > MaxDescriptionLength)
                    throw new RequestInputException(localizer.Get("InvalidDeletionDescriptionLength") + $" {DeletionDescription.Length}" + localizer.Get("MustBeBetween") + $" {MinDescriptionLength} " + localizer.Get("And") + $" {MaxDescriptionLength}");

                await QueryValidationHelper.CheckUserExistsAsync(dbContext, UserId);

                if (QueryValidationHelper.IsReservedGuid(ImageId))
                    throw new RequestInputException("InvalidImageId");

                var image = await dbContext.Images.Include(img => img.Cards).SingleAsync(img => img.Id == ImageId);
                if (image.Cards.Any())
                    throw new RequestInputException(localizer.Get("ImageUsedInCardsPart1") + ' ' + image.Cards.Count() + ' ' + localizer.Get("ImageUsedInCardsPart2"));
            }
        }
        #endregion
    }
}