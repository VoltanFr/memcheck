using MemCheck.Application.QueryValidation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Decks;

public sealed class GetUserDecksWithTags : RequestRunner<GetUserDecksWithTags.Request, IEnumerable<GetUserDecksWithTags.Result>>
{
    public GetUserDecksWithTags(CallContext callContext) : base(callContext)
    {
    }
    protected override async Task<ResultWithMetrologyProperties<IEnumerable<Result>>> DoRunAsync(Request request)
    {
        var userDecks = await DbContext.Decks.AsNoTracking().Where(deck => deck.Owner.Id == request.UserId).Select(deck => new { deckId = deck.Id, deckDescription = deck.Description }).ToListAsync();

        var result = new List<Result>();
        foreach (var userDeck in userDecks)
        {
            var appTags = await new GetTagsOfDeck(DbContext).RunAsync(new GetTagsOfDeck.Request(request.UserId, userDeck.deckId));
            var resultTags = appTags.Select(tag => new ResultTag(tag.TagId, tag.TagName));
            result.Add(new Result(userDeck.deckId, userDeck.deckDescription, resultTags));
        }
        return new ResultWithMetrologyProperties<IEnumerable<Result>>(result, IntMetric("DeckCount", result.Count));
    }
    #region Request & Result
    public sealed record Request(Guid UserId) : IRequest
    {
        public async Task CheckValidityAsync(CallContext callContext)
        {
            await QueryValidationHelper.CheckUserExistsAsync(callContext.DbContext, UserId);
        }
    }
    public sealed record Result(Guid DeckId, string Description, IEnumerable<ResultTag> Tags);
    public sealed record ResultTag(Guid TagId, string TagName);
    #endregion
}
