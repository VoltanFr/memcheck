using MemCheck.Application.QueryValidation;
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
            .Select(img => new ResultImage(img.Id, img.Name))
            .ToImmutableArray();
        var pageImageIds = pageImages.Select(resultImage => resultImage.ImageId).ToImmutableHashSet();
        var cardCounts = DbContext.ImagesInCards.AsNoTracking().Where(imageInCard => pageImageIds.Contains(imageInCard.ImageId)).Select(imageInCard => imageInCard.ImageId).ToImmutableArray();
        var cardCountPerImageId = cardCounts.GroupBy(imageInCard => imageInCard).ToImmutableDictionary(imageCardGroup => imageCardGroup.Key, imageCardGroup => imageCardGroup.Count());

        var result = new Result(totalCount, pageCount, pageImages.Select(img => img with { CardCount = cardCountPerImageId.TryGetValue(img.ImageId, out var value) ? value : 0 }).ToImmutableArray());

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
                throw new PageIndexTooSmallException(PageNo);
            if (PageSize < 1)
                throw new PageSizeTooSmallException(PageSize, 1, MaxPageSize);
            if (PageSize > MaxPageSize)
                throw new PageSizeTooBigException(PageSize, 1, MaxPageSize);
            if (Filter != Filter.Trim())
                throw new TextNotTrimmedException();
            await Task.CompletedTask;
        }
    }
    public sealed record Result(int TotalCount, int PageCount, ImmutableArray<ResultImage> Images);
    public sealed record ResultImage(Guid ImageId, string ImageName)
    {
        public int CardCount { get; internal set; }
    }
    #endregion
}
