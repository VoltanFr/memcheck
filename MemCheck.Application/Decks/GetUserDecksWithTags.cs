using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Decks
{
    public sealed class GetUserDecksWithTags
    {
        #region Fields
        private readonly CallContext callContext;
        #endregion
        public GetUserDecksWithTags(CallContext callContext)
        {
            this.callContext = callContext;
        }
        public async Task<IEnumerable<Result>> RunAsync(Request request)
        {
            await request.CheckValidityAsync(callContext.DbContext);

            var userDecks = callContext.DbContext.Decks.AsNoTracking().Where(deck => deck.Owner.Id == request.UserId).Select(deck => new { deckId = deck.Id, deckDescription = deck.Description }).ToList();

            var result = new List<Result>();
            foreach (var userDeck in userDecks)
            {
                var appTags = await new GetTagsOfDeck(callContext).RunAsync(new GetTagsOfDeck.Request(request.UserId, userDeck.deckId));
                var resultTags = appTags.Select(tag => new ResultTag(tag.TagId, tag.TagName));
                result.Add(new Result(userDeck.deckId, userDeck.deckDescription, resultTags));
            }
            callContext.TelemetryClient.TrackEvent("GetUserDecksWithTags", ("DeckCount", result.Count.ToString()));
            return result;
        }
        #region Request & Result
        public sealed record Request(Guid UserId)
        {
            public async Task CheckValidityAsync(MemCheckDbContext dbContext)
            {
                await QueryValidationHelper.CheckUserExistsAsync(dbContext, UserId);
            }
        }
        public sealed record Result(Guid DeckId, string Description, IEnumerable<ResultTag> Tags);
        public sealed record ResultTag(Guid TagId, string TagName);
        #endregion
    }
}
