using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Languages
{
    public sealed class GetAllLanguages
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public GetAllLanguages(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task<IEnumerable<Result>> RunAsync()
        {
            return await dbContext.CardLanguages.Select(language => new Result(language.Id, language.Name, dbContext.Cards.Where(card => card.CardLanguage.Id == language.Id).Count())).ToListAsync();
        }
        public sealed record Result(Guid Id, string Name, int CardCount);
    }
}
