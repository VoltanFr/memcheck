using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MemCheck.Application
{
    public sealed class GetImageList
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public GetImageList(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public ResultModel Run(int pageSize, int pageNo, string filter)
        {
            var images = dbContext.Images.Include(img => img.Cards);
            var imagesWithFilter = images.Where(image =>
                EF.Functions.Like(image.Name, $"%{filter}%")
                || EF.Functions.Like(image.Description, $"%{filter}%")
                || EF.Functions.Like(image.Source, $"%{filter}%")
                );
            var ordered = imagesWithFilter.OrderBy(image => image.Name);
            var totalCount = ordered.Count();
            var pageCount = (int)Math.Ceiling(((double)totalCount) / pageSize);
            var pageImages = ordered.Skip((pageNo - 1) * pageSize).Take(pageSize);
            return new ResultModel(totalCount, pageCount,
                pageImages.Select(img => new ResultImageModel(
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
        }
        #region Result classes
        public sealed class ResultModel
        {
            public ResultModel(int totalCount, int pageCount, IEnumerable<ResultImageModel> images)
            {
                TotalCount = totalCount;
                PageCount = pageCount;
                Images = images;
            }
            public int TotalCount { get; }
            public int PageCount { get; }
            public IEnumerable<ResultImageModel> Images { get; }
        }
        public sealed class ResultImageModel
        {
            public ResultImageModel(Guid imageId, string imageName, int cardCount, string originalImageContentType,
                string uploader, string description, string source, int originalImageSize, int smallSize, int mediumSize, int bigSize,
                DateTime initialUploadUtcDate, DateTime lastChangeUtcDate, string currentVersionDescription)
            {
                ImageId = imageId;
                ImageName = imageName;
                CardCount = cardCount;
                OriginalImageContentType = originalImageContentType;
                Uploader = uploader;
                Description = description;
                Source = source;
                OriginalImageSize = originalImageSize;
                SmallSize = smallSize;
                MediumSize = mediumSize;
                BigSize = bigSize;
                InitialUploadUtcDate = initialUploadUtcDate;
                LastChangeUtcDate = lastChangeUtcDate;
                CurrentVersionDescription = currentVersionDescription;
            }
            public Guid ImageId { get; }
            public string ImageName { get; } = null!;
            public int CardCount { get; }
            public string OriginalImageContentType { get; }
            public string Uploader { get; } = null!; //aka original version creator
            public string Description { get; } = null!;
            public string Source { get; } = null!;
            public int OriginalImageSize { get; }
            public int SmallSize { get; }
            public int MediumSize { get; }
            public int BigSize { get; }
            public DateTime InitialUploadUtcDate { get; }
            public DateTime LastChangeUtcDate { get; }
            public string CurrentVersionDescription { get; } = null!;
        }
        #endregion
    }
}
