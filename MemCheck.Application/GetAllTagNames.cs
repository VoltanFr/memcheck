using MemCheck.Database;
using System.Collections.Generic;
using System.Linq;

namespace MemCheck.Application
{
    public sealed class GetAllTagNames
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public GetAllTagNames(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public IEnumerable<string> Run()
        {
            return dbContext.Tags.Select(tag => tag.Name);
        }
    }
}

