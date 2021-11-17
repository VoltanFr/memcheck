using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Notifying
{
    public sealed class RemoveCardSubscriptions
    {
        #region Fields
        private readonly CallContext callContext;
        #endregion
        public RemoveCardSubscriptions(CallContext callContext)
        {
            this.callContext = callContext;
        }
        public async Task RunAsync(Request request)
        {
            request.CheckValidity();

            foreach (var cardId in request.CardIds)
            {
                var existing = await callContext.DbContext.CardNotifications.Where(notif => notif.UserId == request.UserId && notif.CardId == cardId).SingleOrDefaultAsync();
                if (existing != null)
                    callContext.DbContext.CardNotifications.Remove(existing);
            }

            await callContext.DbContext.SaveChangesAsync();
            callContext.TelemetryClient.TrackEvent("RemoveCardSubscriptions", ("CardCount", request.CardIds.Count().ToString()));
        }
        #region Request class
        public sealed class Request
        {
            public Request(Guid userId, IEnumerable<Guid> cardIds)
            {
                UserId = userId;
                CardIds = cardIds;
            }
            public Guid UserId { get; }
            public IEnumerable<Guid> CardIds { get; }
            public void CheckValidity()
            {
                if (QueryValidationHelper.IsReservedGuid(UserId))
                    throw new RequestInputException("Invalid user id");
                if (CardIds.Any(cardId => QueryValidationHelper.IsReservedGuid(cardId)))
                    throw new RequestInputException($"Invalid card id");
            }
        }
        #endregion
    }
}
