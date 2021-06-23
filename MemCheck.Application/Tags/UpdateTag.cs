using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace MemCheck.Application.Tags
{
    public sealed class UpdateTag
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public UpdateTag(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task RunAsync(Request request, ILocalized localizer)
        {
            await request.CheckValidityAsync(localizer, dbContext);

            var tag = await dbContext.Tags.SingleAsync(tag => tag.Id == request.TagId);
            tag.Name = request.NewName;
            tag.Description = request.NewDescription;
            await dbContext.SaveChangesAsync();
        }
        #region Request type
        public sealed record Request(Guid UserId, Guid TagId, string NewName, string NewDescription)
        {
            public async Task CheckValidityAsync(ILocalized localizer, MemCheckDbContext dbContext)
            {
                await QueryValidationHelper.CheckUserExistsAsync(dbContext, UserId);
                await QueryValidationHelper.CheckCanCreateTag(NewName, NewDescription, TagId, dbContext, localizer);
            }
        }
        #endregion
    }
}
