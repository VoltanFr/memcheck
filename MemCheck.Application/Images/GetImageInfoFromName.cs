using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Images
{
    public sealed class GetImageInfoFromName
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public GetImageInfoFromName(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task<Result> RunAsync(Request request, ILocalized localizer)
        {
            await request.CheckValidity(dbContext, localizer);
            var img = await dbContext.Images.Include(img => img.Cards)
                .Where(image => EF.Functions.Like(image.Name, $"{request.ImageName}"))
                .Select(img => new { img.Id, img.Name, img.Source })
                .SingleAsync();
            return new Result(img.Id, img.Name, img.Source);
        }
        #region Request and Result types
        public sealed record Request(string ImageName)
        {
            public async Task CheckValidity(MemCheckDbContext dbContext, ILocalized localizer)
            {
                if (ImageName != ImageName.Trim())
                    throw new InvalidOperationException($"Name not trimmed: '{ImageName}'");
                if (ImageName.Length == 0)
                    throw new RequestInputException(localizer.Get("PleaseEnterAnImageName"));

                if (!await dbContext.Images.AnyAsync(image => EF.Functions.Like(image.Name, $"{ImageName}")))
                    throw new RequestInputException(localizer.Get("ImageNotFound") + ' ' + ImageName);

            }
        }
        public sealed record Result(Guid ImageId, string Name, string Source);
        #endregion
    }
}
