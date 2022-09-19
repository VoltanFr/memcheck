using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Images;

public sealed class GetImageList : RequestRunner<GetImageList.Request, GetImageList.Result>
{
    #region Fields
    #endregion
    public GetImageList(CallContext callContext) : base(callContext)
    {
    }
    protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
    {
        var images = DbContext.Images.AsNoTracking()
            .Where(image => string.IsNullOrEmpty(request.Filter) || EF.Functions.Like(image.Name, $"%{request.Filter}%") || EF.Functions.Like(image.Description, $"%{request.Filter}%") || EF.Functions.Like(image.Source, $"%{request.Filter}%"))
            .OrderBy(image => image.Name);
        var totalCount = await images.CountAsync();
        var pageCount = (int)Math.Ceiling((double)totalCount / request.PageSize);
        var pageImages = images
            .Skip((request.PageNo - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(img => new ResultImage(
                            img.Id,
                            img.Name,
                            img.OriginalContentType,
                            img.Owner.UserName,
                            img.Description,
                            img.Source,
                            img.OriginalSize,
                            img.SmallBlobSize,
                            img.MediumBlobSize,
                            img.BigBlobSize,
                            img.InitialUploadUtcDate,
                            img.LastChangeUtcDate,
                            img.VersionDescription
                            ))
            .ToImmutableArray();
        var pageImageIds = pageImages.Select(resultImage => resultImage.ImageId).ToImmutableHashSet();
        var cardCounts = DbContext.ImagesInCards.AsNoTracking().Where(imageInCard => pageImageIds.Contains(imageInCard.ImageId)).Select(imageInCard => imageInCard.ImageId).ToImmutableArray();
        var cardCountPerImageId = cardCounts.GroupBy(imageInCard => imageInCard).ToImmutableDictionary(imageCardGroup => imageCardGroup.Key, imageCardGroup => imageCardGroup.Count());

        var result = new Result(totalCount, pageCount, pageImages.Select(img => img with { CardCount = cardCountPerImageId.ContainsKey(img.ImageId) ? cardCountPerImageId[img.ImageId] : 0 }).ToImmutableArray());

        return new ResultWithMetrologyProperties<Result>(result,
            IntMetric("PageSize", request.PageSize),
            IntMetric("PageNo", request.PageNo),
            ("Filter", request.Filter),
            IntMetric("ResultTotalCount", result.TotalCount),
            IntMetric("ResultPageCount", result.PageCount),
            IntMetric("ResultImageCount", result.Images.Length));
    }
    #region Request & Result types
    public sealed record Request(int PageSize, int PageNo, string Filter) : IRequest
    {
        public const int MaxPageSize = 100;
        public async Task CheckValidityAsync(CallContext callContext)
        {
            if (PageNo < 1)
                throw new InvalidOperationException($"First page is numbered 1, received a request for page {PageNo}");
            if (PageSize < 1)
                throw new InvalidOperationException($"PageSize too small: {PageSize} (max size: {MaxPageSize})");
            if (PageSize > MaxPageSize)
                throw new InvalidOperationException($"PageSize too big: {PageSize} (max size: {MaxPageSize})");
            await Task.CompletedTask;
        }
    }
    public sealed record Result(int TotalCount, int PageCount, ImmutableArray<ResultImage> Images);
    public sealed record ResultImage(Guid ImageId, string ImageName, string OriginalImageContentType,
            string Uploader, string Description, string Source, int OriginalImageSize, int SmallSize, int MediumSize, int BigSize,
            DateTime InitialUploadUtcDate, DateTime LastChangeUtcDate, string CurrentVersionDescription)
    {
        public int CardCount { get; internal set; }
    }
    #endregion
}
