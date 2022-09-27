using MemCheck.Application.QueryValidation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Images;

public sealed class GetImageInfoFromName : RequestRunner<GetImageInfoFromName.Request, GetImageInfoFromName.Result>
{
    #region Fields
    #endregion
    public GetImageInfoFromName(CallContext callContext) : base(callContext)
    {
    }
    protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
    {
        var img = await DbContext.Images
            .AsNoTracking()
            .Where(image => EF.Functions.Like(image.Name, $"{request.ImageName}"))
            .Select(img => new { img.Id, img.Description, img.Source, img.InitialUploadUtcDate, img.Owner.UserName, img.LastChangeUtcDate, img.VersionDescription, img.OriginalContentType, img.OriginalSize, img.SmallBlobSize, img.MediumBlobSize, img.BigBlobSize })
            .SingleAsync();

        var cardCount = await DbContext.ImagesInCards
            .AsNoTracking()
            .Where(imageInCard => imageInCard.ImageId == img.Id)
            .CountAsync();

        var result = new Result(img.Id, img.Description, img.Source, img.InitialUploadUtcDate, img.UserName, img.LastChangeUtcDate, img.VersionDescription, img.OriginalContentType, img.OriginalSize, img.SmallBlobSize, img.MediumBlobSize, img.BigBlobSize, cardCount);

        return new ResultWithMetrologyProperties<Result>(result, ("ImageName", request.ImageName));
    }
    #region Request and Result types
    public sealed record Request(string ImageName) : IRequest
    {
        public async Task CheckValidityAsync(CallContext callContext)
        {
            QueryValidationHelper.CheckImageNameValidity(ImageName, callContext.Localized);

            if (!await callContext.DbContext.Images.AnyAsync(image => EF.Functions.Like(image.Name, $"{ImageName}")))
                throw new ImageNotFoundException(callContext.Localized.GetLocalized("ImageNotFound") + ' ' + ImageName);

        }
    }
    public sealed record Result(Guid Id, string Description, string Source, DateTime InitialUploadUtcDate, string InitialVersionCreator, DateTime CurrentVersionUtcDate, string CurrentVersionDescription, string OriginalImageContentType, int OriginalImageSize, int SmallSize, int MediumSize, int BigSize, int CardCount);
    #endregion
}
