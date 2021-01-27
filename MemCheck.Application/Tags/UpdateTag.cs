using MemCheck.Database;
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
        public async Task<bool> RunAsync(Guid tagId, string newName)
        {
            newName = newName.Trim();
            //CreateTag.CheckNameValidity(dbContext, newName);

            var tag = dbContext.Tags.Single(tag => tag.Id == tagId);
            tag.Name = newName;
            await dbContext.SaveChangesAsync();
            return true;
        }
        public sealed class Request
        {
            public Guid DeckId { get; set; }
            public string Description { get; set; } = null!;
            public int HeapingAlgorithmId { get; set; }
        }
    }
}
