using MemCheck.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MemCheck.Application
{
    public sealed class GetAllCardsInDb
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public GetAllCardsInDb(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public IEnumerable<ViewModel> Run()
        {
            return dbContext.Cards.Select(card => new ViewModel() { Id = card.Id, FrontSide = card.FrontSide, BackSide = card.BackSide });
        }

        public sealed class ViewModel
        {
            public Guid Id { get; set; }
            public string FrontSide { get; set; } = null!;
            public string BackSide { get; set; } = null!;
        }
    }
}
