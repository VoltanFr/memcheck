using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Decks
{
    internal sealed class GetTagsOfDeck
    {
        #region Fields
        private readonly CallContext callContext;
        #endregion
        public GetTagsOfDeck(CallContext callContext)
        {
            this.callContext = callContext;
        }
        public async Task<IEnumerable<Result>> RunAsync(Request request)
        {
            await request.CheckValidityAsync(callContext.DbContext);
            var result = callContext.DbContext.CardsInDecks
                            .AsNoTracking()
                            .Include(cardInDeck => cardInDeck.Card.TagsInCards)
                            .ThenInclude(tagInCard => tagInCard.Tag)
                            .Where(cardInDeck => cardInDeck.DeckId == request.DeckId)
                            .SelectMany(cardInDeck => cardInDeck.Card.TagsInCards)
                            .Select(tagInCard => tagInCard.Tag)
                            .Distinct()
                            .Select(tag => new Result(tag.Id, tag.Name))
                            .ToList()
                            .OrderBy(resultModel => resultModel.TagName);
            callContext.TelemetryClient.TrackEvent("GetTagsOfDeck", ("TagCount", result.Count().ToString()));
            return result;
        }
        #region Request & Result
        public sealed record Request(Guid UserId, Guid DeckId)
        {
            public async Task CheckValidityAsync(MemCheckDbContext dbContext)
            {
                await QueryValidationHelper.CheckUserExistsAsync(dbContext, UserId);
                await QueryValidationHelper.CheckUserIsOwnerOfDeckAsync(dbContext, UserId, DeckId);
            }
        }
        public sealed record Result(Guid TagId, string TagName);
        #endregion
    }
}