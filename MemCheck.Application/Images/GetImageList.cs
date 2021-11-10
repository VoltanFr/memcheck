using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Images
{
    public sealed class GetImageList
    {
        #region Fields
        private readonly CallContext callContext;
        #endregion
        public GetImageList(CallContext callContext)
        {
            this.callContext = callContext;
        }
        public async Task<Result> RunAsync(Request request)
        {
            request.CheckValidity();

            IQueryable<Domain.Image>? images = callContext.DbContext.Images.AsNoTracking().Include(img => img.Cards);
            if (request.Filter.Length > 0)
                images = images.Where(image =>
                    EF.Functions.Like(image.Name, $"%{request.Filter}%")
                    || EF.Functions.Like(image.Description, $"%{request.Filter}%")
                    || EF.Functions.Like(image.Source, $"%{request.Filter}%")
                    );
            var ordered = images.OrderBy(image => image.Name);
            var totalCount = await ordered.CountAsync();
            var pageCount = (int)Math.Ceiling((double)totalCount / request.PageSize);
            var pageImages = ordered.Skip((request.PageNo - 1) * request.PageSize).Take(request.PageSize);
            var result = new Result(totalCount, pageCount,
                            pageImages.Select(img => new ResultImage(
                                img.Id,
                                img.Name,
                                img.Cards.Count(),
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
                                )
                            )
                        );
            callContext.TelemetryClient.TrackEvent("GetImageList",
                ("PageSize", request.PageSize.ToString()),
                ("PageNo", request.PageNo.ToString()),
                ("Filter", request.Filter),
                ("ResultTotalCount", result.TotalCount.ToString()),
                ("ResultPageCount", result.PageCount.ToString()),
                ("ResultImageCount", result.Images.Count().ToString()));
            return result;
        }
        #region Request & Result types
        public sealed record Request(int PageSize, int PageNo, string Filter)
        {
            public const int MaxPageSize = 100;
            public void CheckValidity()
            {
                if (PageNo < 1)
                    throw new InvalidOperationException($"First page is numbered 1, received a request for page {PageNo}");
                if (PageSize < 1)
                    throw new InvalidOperationException($"PageSize too small: {PageSize} (max size: {MaxPageSize})");
                if (PageSize > MaxPageSize)
                    throw new InvalidOperationException($"PageSize too big: {PageSize} (max size: {MaxPageSize})");
            }
        }
        public sealed record Result(int TotalCount, int PageCount, IEnumerable<ResultImage> Images);
        public sealed record ResultImage(Guid ImageId, string ImageName, int CardCount, string OriginalImageContentType,
                string Uploader, string Description, string Source, int OriginalImageSize, int SmallSize, int MediumSize, int BigSize,
                DateTime InitialUploadUtcDate, DateTime LastChangeUtcDate, string CurrentVersionDescription);
        #endregion
    }
}
