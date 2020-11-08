using MemCheck.Database;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MemCheck.Application
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
        public IEnumerable<ViewModel> Run()
        {
            return dbContext.CardLanguages.Select(language => new ViewModel(language.Id, language.Name, dbContext.Cards.Where(card => card.CardLanguage.Id == language.Id).Count()));
        }
        public sealed class ViewModel
        {
            public ViewModel(Guid Id, string Name, int CardCount)
            {
                this.Id = Id;
                this.Name = Name;
                this.CardCount = CardCount;
            }

            public Guid Id { get; }
            public string Name { get; }
            public int CardCount { get; }
        }
    }
}
