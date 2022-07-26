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
        var result = await DbContext.Images.Include(img => img.Cards)
            .Where(image => EF.Functions.Like(image.Name, $"{request.ImageName}"))
            .Select(img => new Result(img.Id, img.Description, img.Source, img.InitialUploadUtcDate, img.Owner.UserName, img.LastChangeUtcDate, img.VersionDescription, img.Cards.Count(), img.OriginalContentType, img.OriginalSize, img.SmallBlobSize, img.MediumBlobSize, img.BigBlobSize))
            .SingleAsync();
        return new ResultWithMetrologyProperties<Result>(result, ("ImageName", request.ImageName));
    }
    #region Request and Result types
    public sealed record Request(string ImageName) : IRequest
    {
        public async Task CheckValidityAsync(CallContext callContext)
        {
            if (ImageName != ImageName.Trim())
                throw new InvalidOperationException($"Name not trimmed: '{ImageName}'");
            if (ImageName.Length == 0)
                throw new RequestInputException(callContext.Localized.GetLocalized("PleaseEnterAnImageName"));

            if (!await callContext.DbContext.Images.AnyAsync(image => EF.Functions.Like(image.Name, $"{ImageName}")))
                throw new RequestInputException(callContext.Localized.GetLocalized("ImageNotFound") + ' ' + ImageName);

        }
    }
    public sealed record Result(Guid Id, string Description, string Source, DateTime InitialUploadUtcDate, string InitialVersionCreator, DateTime CurrentVersionUtcDate, string CurrentVersionDescription, int CardCount, string OriginalImageContentType, int OriginalImageSize, int SmallSize, int MediumSize, int BigSize);
    #endregion
}
