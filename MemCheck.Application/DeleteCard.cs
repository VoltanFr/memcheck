using MemCheck.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemCheck.Application
{
    public sealed class DeleteCard  //Not used
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public DeleteCard(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task<bool> RunAsync(Guid id)
        {
            var card = dbContext.Cards.FirstOrDefault(card => card.Id.Equals(id));
            dbContext.Cards.Remove(card);
            await dbContext.SaveChangesAsync();
            return true;
        }
    }
}
