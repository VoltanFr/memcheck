using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
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
            await dbContext.SaveChangesAsync();
        }
        #region Request type
        public sealed record Request(Guid TagId, string NewName)
        {
            public async Task CheckValidityAsync(ILocalized localizer, MemCheckDbContext dbContext)
            {
                await QueryValidationHelper.CheckCanCreateTagWithName(NewName, dbContext, localizer);
            }
        }
        #endregion
    }
}
