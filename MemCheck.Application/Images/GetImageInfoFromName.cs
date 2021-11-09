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
        private readonly CallContext callContext;
        #endregion
        public GetImageInfoFromName(CallContext callContext)
        {
            this.callContext = callContext;
        }
        public async Task<Result> RunAsync(Request request, ILocalized localizer)
        {
            await request.CheckValidity(callContext.DbContext, localizer);
            var img = await callContext.DbContext.Images.Include(img => img.Cards)
                .Where(image => EF.Functions.Like(image.Name, $"{request.ImageName}"))
                .Select(img => new { img.Id, img.Name, img.Source })
                .SingleAsync();
            var result = new Result(img.Id, img.Name, img.Source);
            callContext.TelemetryClient.TrackEvent("GetImageInfoFromName", ("ImageName", request.ImageName));
            return result;
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
