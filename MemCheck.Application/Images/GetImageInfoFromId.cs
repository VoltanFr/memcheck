using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Images
{
    public sealed class GetImageInfoFromId
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public GetImageInfoFromId(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task<Result> RunAsync(Request request)
        {
            request.CheckValidity();
            return await dbContext.Images
                .AsNoTracking()
                .Where(img => img.Id == request.ImageId)
                .Select(img => new Result(img.Owner, img.Name, img.Description, img.Source, img.Cards.Count(), img.InitialUploadUtcDate, img.LastChangeUtcDate, img.VersionDescription))
                .SingleAsync();
        }
        #region Request and Result
        public sealed record Request(Guid ImageId)
        {
            public void CheckValidity()
            {
                QueryValidationHelper.CheckNotReservedGuid(ImageId);
            }
        }
        public sealed record Result(MemCheckUser Owner, string Name, string Description, string Source, int CardCount, DateTime InitialUploadUtcDate, DateTime LastChangeUtcDate, string CurrentVersionDescription);
        #endregion
    }
}
