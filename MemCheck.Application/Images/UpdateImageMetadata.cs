using MemCheck.Application.QueryValidation;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Images
{
    public sealed class UpdateImageMetadata : RequestRunner<UpdateImageMetadata.Request, UpdateImageMetadata.Result>
    {
        private readonly DateTime? runDate;
        #region Fields
        #endregion
        #region Private methods
        private static ImagePreviousVersionType ImagePreviousVersionTypeFromImage(Domain.Image i)
        {
            return i.VersionType switch {
                ImageVersionType.Creation => ImagePreviousVersionType.Creation,
                ImageVersionType.Changes => ImagePreviousVersionType.Changes,
                _ => throw new NotImplementedException(),
            };
        }
        #endregion
        public UpdateImageMetadata(CallContext callContext, DateTime? runDate = null) : base(callContext)
        {
            this.runDate = runDate;
        }
        protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
        {
            var image = await DbContext.Images.Include(img => img.Owner).Include(img => img.PreviousVersion).SingleAsync(img => img.Id == request.ImageId);
            var user = await DbContext.Users.SingleAsync(u => u.Id == request.UserId);

            var versionFromCurrentImage = new ImagePreviousVersion() {
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

            DbContext.ImagePreviousVersions.Add(versionFromCurrentImage);

            image.Owner = user;
            image.Name = request.Name;
            image.Description = request.Description;
            image.Source = request.Source;
            image.LastChangeUtcDate = runDate ?? DateTime.UtcNow;
            image.VersionType = ImageVersionType.Changes;
            image.VersionDescription = request.VersionDescription;
            image.PreviousVersion = versionFromCurrentImage;

            await DbContext.SaveChangesAsync();

            return new ResultWithMetrologyProperties<Result>(new Result(),
                ("ImageId", request.ImageId.ToString()),
                ("NewName", request.Name),
                IntMetric("NewNameLength", request.Name.Length),
                IntMetric("DescriptionLength", request.Description.Length),
                IntMetric("SourceFieldLength", request.Source.Length),
                IntMetric("VersionDescriptionLength", request.VersionDescription.Length));
        }
        #region Request & Result
        public sealed class Request : IRequest
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
            public async Task CheckValidityAsync(CallContext callContext)
            {
                if (QueryValidationHelper.IsReservedGuid(UserId))
                    throw new InvalidOperationException("Invalid user id");
                if (QueryValidationHelper.IsReservedGuid(ImageId))
                    throw new InvalidOperationException("Invalid image id");

                var imageDataBeforeUpdate = await callContext.DbContext.Images
                    .AsNoTracking()
                    .Where(img => img.Id == ImageId)
                    .Select(img => new { nameBeforeUpdate = img.Name, sourceBeforeUpdate = img.Source, descriptionBeforeUpdate = img.Description })
                    .SingleAsync();

                if (imageDataBeforeUpdate.nameBeforeUpdate == Name && imageDataBeforeUpdate.sourceBeforeUpdate == Source && imageDataBeforeUpdate.descriptionBeforeUpdate == Description)
                    throw new RequestInputException(callContext.Localized.GetLocalized("CanNotUpdateMetadataBecauseSameAsOriginal"));

                if (imageDataBeforeUpdate.nameBeforeUpdate != Name)
                    await QueryValidationHelper.CheckCanCreateImageWithNameAsync(Name, callContext.DbContext, callContext.Localized);

                QueryValidationHelper.CheckCanCreateImageWithSource(Source, callContext.Localized);
                QueryValidationHelper.CheckCanCreateImageWithDescription(Description, callContext.Localized);
                QueryValidationHelper.CheckCanCreateImageWithVersionDescription(VersionDescription, callContext.Localized);
            }
        }
        public sealed record Result();
        #endregion
    }
}
