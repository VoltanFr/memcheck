using MemCheck.Application.QueryValidation;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace MemCheck.Application.Decks
{
    public sealed class CreateDeck : RequestRunner<CreateDeck.Request, CreateDeck.Result>
    {
        public CreateDeck(CallContext callContext) : base(callContext)
        {
        }
        protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
        {
            var user = await DbContext.Users.SingleAsync(user => user.Id == request.UserId);
            var deck = new Deck() { Owner = user, Description = request.Name, HeapingAlgorithmId = request.HeapingAlgorithmId };
            DbContext.Decks.Add(deck);
            await DbContext.SaveChangesAsync();
            return new ResultWithMetrologyProperties<Result>(new Result());
        }
        #region Request type
        public sealed record Request(Guid UserId, string Name, int HeapingAlgorithmId) : IRequest
        {
            public async Task CheckValidityAsync(CallContext callContext)
            {
                await QueryValidationHelper.CheckCanCreateDeckAsync(UserId, Name, HeapingAlgorithmId, callContext.DbContext, callContext.Localized);
            }
        }
        public sealed record Result();
        #endregion

    }
}
