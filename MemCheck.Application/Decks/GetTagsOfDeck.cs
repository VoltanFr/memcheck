using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Decks
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
        public async Task<IEnumerable<ResultModel>> RunAsync(Request request)
        {
            await request.CheckValidityAsync(dbContext);
            return dbContext.CardsInDecks
                .AsNoTracking()
                .Include(cardInDeck => cardInDeck.Card.TagsInCards)
                .ThenInclude(tagInCard => tagInCard.Tag)
                .Where(cardInDeck => cardInDeck.DeckId == request.DeckId)
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
        #region Request & Result
        public sealed record Request(Guid UserId, Guid DeckId)
        {
            public async Task CheckValidityAsync(MemCheckDbContext dbContext)
            {
                await QueryValidationHelper.CheckUserExistsAsync(dbContext, UserId);
            }
        }
        public sealed record Result(Guid DeckId, string Description, int HeapingAlgorithmId, int CardCount);
        #endregion
    }
}