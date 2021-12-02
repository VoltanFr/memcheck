using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Notifying
{
    public sealed class RemoveCardSubscriptions : RequestRunner<RemoveCardSubscriptions.Request, RemoveCardSubscriptions.Result>
    {
        public RemoveCardSubscriptions(CallContext callContext) : base(callContext)
        {
        }
        protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
        {
            foreach (var cardId in request.CardIds)
            {
                var existing = await DbContext.CardNotifications.Where(notif => notif.UserId == request.UserId && notif.CardId == cardId).SingleOrDefaultAsync();
                if (existing != null)
                    DbContext.CardNotifications.Remove(existing);
            }

            await DbContext.SaveChangesAsync();
            return new ResultWithMetrologyProperties<Result>(new Result(), ("CardCount", request.CardIds.Count().ToString()));
        }
        #region Request & Result
        public sealed class Request : IRequest
        {
            public Request(Guid userId, IEnumerable<Guid> cardIds)
            {
                UserId = userId;
                CardIds = cardIds;
            }
            public Guid UserId { get; }
            public IEnumerable<Guid> CardIds { get; }
            public async Task CheckValidityAsync(CallContext callContext)
            {
                if (QueryValidationHelper.IsReservedGuid(UserId))
                    throw new RequestInputException("Invalid user id");
                if (CardIds.Any(cardId => QueryValidationHelper.IsReservedGuid(cardId)))
                    throw new RequestInputException($"Invalid card id");
                await Task.CompletedTask;
            }
        }
        public sealed record Result();
        #endregion
    }
}
