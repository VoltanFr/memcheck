using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Notifying
{
    public sealed class DeleteSearchSubscription
    {
        #region Fields
        private readonly CallContext callContext;
        #endregion
        public DeleteSearchSubscription(CallContext callContext)
        {
            this.callContext = callContext;
        }
        public async Task RunAsync(Request request)
        {
            await request.CheckValidityAsync(callContext.DbContext);
            var subscription = await callContext.DbContext.SearchSubscriptions.Where(s => s.Id == request.SubscriptionId).SingleAsync();
            callContext.DbContext.SearchSubscriptions.Remove(subscription);
            await callContext.DbContext.SaveChangesAsync();
            callContext.TelemetryClient.TrackEvent("DeleteSearchSubscription", ("subscriptionId", request.SubscriptionId.ToString()));
        }
        #region Request class
        public sealed class Request
        {
            public Request(Guid userId, Guid subscriptionId)
            {
                UserId = userId;
                SubscriptionId = subscriptionId;
            }
            public Guid UserId { get; }
            public Guid SubscriptionId { get; }
            public async Task CheckValidityAsync(MemCheckDbContext dbContext)
            {
                QueryValidationHelper.CheckNotReservedGuid(UserId);
                QueryValidationHelper.CheckNotReservedGuid(SubscriptionId);
                var subscription = await dbContext.SearchSubscriptions.Where(s => s.Id == SubscriptionId).SingleOrDefaultAsync();
                if (subscription == null)
                    throw new RequestInputException("Subscription not found");
                if (subscription.UserId != UserId)
                    throw new RequestInputException("User not owner of subscription");
            }
        }
        #endregion
    }
}
