using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Tags
{
    public sealed class GetTag
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public GetTag(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task<Result> RunAsync(Request request)
        {
            var tag = await dbContext.Tags.AsNoTracking().Include(tag => tag.TagsInCards).SingleAsync(tag => tag.Id == request.TagId);
            return new Result(tag.Id, tag.Name, tag.TagsInCards == null ? 0 : tag.TagsInCards.Count);
        }
        #region Request type
        public sealed record Request(Guid TagId)
        {
            public async Task CheckValidityAsync(MemCheckDbContext dbContext)
            {
                QueryValidationHelper.CheckNotReservedGuid(TagId);
                if (!await dbContext.Tags.AsNoTracking().Include(tag => tag.TagsInCards).AnyAsync())
                    throw new InvalidOperationException("Invalid tag ID");
            }
        }
        public sealed record Result(Guid TagId, string TagName, int CardCount);
    }
    #endregion
}
