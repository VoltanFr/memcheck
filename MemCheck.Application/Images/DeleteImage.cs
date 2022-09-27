using MemCheck.Application.QueryValidation;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Images;

public sealed class DeleteImage : RequestRunner<DeleteImage.Request, DeleteImage.Result>
{
    #region Fields
    private readonly DateTime? runDate;
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
    public DeleteImage(CallContext callContext, DateTime? runDate = null) : base(callContext)
    {
        this.runDate = runDate;
    }
    protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
    {
        var image = await DbContext.Images.Where(img => img.Id == request.ImageId).SingleAsync();
        var user = await DbContext.Users.SingleAsync(u => u.Id == request.UserId);

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
            VersionUtcDate = runDate ?? DateTime.UtcNow,
            OriginalContentType = image.OriginalContentType,
            OriginalSize = image.OriginalSize,
            OriginalBlob = image.OriginalBlob,
            VersionType = ImagePreviousVersionType.Deletion,
            VersionDescription = request.DeletionDescription,
            PreviousVersion = versionFromCurrentImage
        };

        DbContext.ImagePreviousVersions.Add(versionFromCurrentImage);
        DbContext.ImagePreviousVersions.Add(deletionVersion);

        DbContext.Images.Remove(image);

        await DbContext.SaveChangesAsync();

        var result = image.Name;
        return new ResultWithMetrologyProperties<Result>(new Result(result), ("ImageId", request.ImageId.ToString()), ("ImageName", result), ("DeletionDescription", request.DeletionDescription));
    }
    #region Request type
    public sealed record Request(Guid UserId, Guid ImageId, string DeletionDescription) : IRequest
    {
        public const int MinDescriptionLength = 3;
        public const int MaxDescriptionLength = 1000;
        public async Task CheckValidityAsync(CallContext callContext)
        {
            await QueryValidationHelper.CheckUserExistsAsync(callContext.DbContext, UserId);
            QueryValidationHelper.CheckCanCreateImageWithVersionDescription(DeletionDescription, callContext.Localized);
            await QueryValidationHelper.CheckImageExistsAsync(callContext.DbContext, ImageId);
            await QueryValidationHelper.CheckImageIsNotUsedByAnyCardAsync(callContext.DbContext, ImageId, callContext.Localized);
        }
    }
    public sealed record Result(string ImageName);
    #endregion
}
