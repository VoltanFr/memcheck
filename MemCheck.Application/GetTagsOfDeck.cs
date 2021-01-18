using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MemCheck.Application
{
    public sealed class GetTagsOfDeck
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public GetTagsOfDeck(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public IEnumerable<ResultModel> Run(Guid deckId)
        {
            return dbContext.CardsInDecks
                .AsNoTracking()
                .Include(cardInDeck => cardInDeck.Card.TagsInCards)
                .ThenInclude(tagInCard => tagInCard.Tag)
                .Where(cardInDeck => cardInDeck.DeckId == deckId)
                .SelectMany(cardInDeck => cardInDeck.Card.TagsInCards)
                .Select(tagInCard => tagInCard.Tag)
                .Distinct()
                .Select(tag => new ResultModel(tag.Id, tag.Name))
                .ToList()
                .OrderBy(resultModel => resultModel.TagName);
        }
        #region Result class
        public sealed class ResultModel
        {
            public ResultModel(Guid tagId, string tagName)
            {
                TagId = tagId;
                TagName = tagName;
            }
            public Guid TagId { get; }
            public string TagName { get; }
        }
        #endregion
    }
}