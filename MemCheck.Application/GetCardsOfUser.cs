using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MemCheck.Application
{
    public sealed class GetCardsOfUser
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public GetCardsOfUser(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public IEnumerable<ViewModel> Run()
        {
            return dbContext.Cards.Include(card => card.TagsInCards).Select(card =>
               new ViewModel(card.Id, card.FrontSide, card.BackSide, card.TagsInCards.Select(tagInCard => tagInCard.Tag.Name))
            );
        }

        public sealed class ViewModel
        {
            public ViewModel(Guid cardId, string frontSide, string backSide, IEnumerable<string> tags)
            {
                CardId = cardId;
                FrontSide = frontSide;
                BackSide = backSide;
                Tags = tags;
            }
            public Guid CardId { get; }
            public string FrontSide { get; }
            public string BackSide { get; }
            public IEnumerable<string> Tags { get; }
        }
    }
}
