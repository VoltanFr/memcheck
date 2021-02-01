using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Images
{
    public sealed class GetImageInfo
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        private readonly ILocalized localizer;
        #endregion
        #region Private methods
        private async Task<Result> ResultFromSearchAsync(IQueryable<Image> searchResult, string additionalInfoForNotFound)
        {
            if (!searchResult.Any())
                throw new RequestInputException(localizer.Get("ImageNotFound") + ' ' + additionalInfoForNotFound);

            var results = searchResult.Select(img => new Result(img.Id, img.Owner, img.Name, img.Description, img.Source, img.Cards.Count(), img.InitialUploadUtcDate, img.LastChangeUtcDate, img.VersionDescription));
            return await results.SingleAsync();
        }
        #endregion
        public GetImageInfo(MemCheckDbContext dbContext, ILocalized localizer)
        {
            this.dbContext = dbContext;
            this.localizer = localizer;
        }
        public async Task<Result> RunAsync(string imageName)
        {
            imageName = imageName.Trim();
            if (imageName.Length == 0)
                throw new RequestInputException(localizer.Get("PleaseEnterAnImageName"));
            var images = dbContext.Images.Include(img => img.Cards).Where(image => EF.Functions.Like(image.Name, $"{imageName}"));
            return await ResultFromSearchAsync(images, imageName);
        }
        public async Task<Result> RunAsync(Guid imageId)
        {
            if (QueryValidationHelper.IsReservedGuid(imageId))
                throw new RequestInputException("Invalid image id");
            var images = dbContext.Images.Where(img => img.Id == imageId);
            return await ResultFromSearchAsync(images, "?");
        }
        public sealed class Result
        {
            public Result(Guid imageId, MemCheckUser owner, string name, string description, string source, int cardCount, DateTime initialUploadUtcDate, DateTime lastChangeUtcDate, string currentVersionDescription)
            {
                ImageId = imageId;
                Owner = owner;
                Name = name;
                Description = description;
                Source = source;
                CardCount = cardCount;
                InitialUploadUtcDate = initialUploadUtcDate;
                LastChangeUtcDate = lastChangeUtcDate;
                CurrentVersionDescription = currentVersionDescription;
            }
            public Guid ImageId { get; }
            public MemCheckUser Owner { get; }
            public string Name { get; }
            public string Description { get; }
            public string Source { get; }
            public int CardCount { get; }
            public DateTime InitialUploadUtcDate { get; }
            public DateTime LastChangeUtcDate { get; }
            public string CurrentVersionDescription { get; }
        }
    }
}
