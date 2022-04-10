using MemCheck.Application.QueryValidation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Notifiying
{
    public sealed class DeleteSearchSubscription : RequestRunner<DeleteSearchSubscription.Request, DeleteSearchSubscription.Result>
    {
        public DeleteSearchSubscription(CallContext callContext) : base(callContext)
        {
        }
        protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
        {
            var subscription = await DbContext.SearchSubscriptions.Where(s => s.Id == request.SubscriptionId).SingleAsync();
            DbContext.SearchSubscriptions.Remove(subscription);
            await DbContext.SaveChangesAsync();
            return new ResultWithMetrologyProperties<Result>(new Result(), ("subscriptionId", request.SubscriptionId.ToString()));
        }
        #region Request & Result
        public sealed class Request : IRequest
        {
            public Request(Guid userId, Guid subscriptionId)
            {
                UserId = userId;
                SubscriptionId = subscriptionId;
            }
            public Guid UserId { get; }
            public Guid SubscriptionId { get; }
            public async Task CheckValidityAsync(CallContext callContext)
            {
                QueryValidationHelper.CheckNotReservedGuid(UserId);
                QueryValidationHelper.CheckNotReservedGuid(SubscriptionId);
                var subscription = await callContext.DbContext.SearchSubscriptions.Where(s => s.Id == SubscriptionId).SingleOrDefaultAsync();
                if (subscription == null)
                    throw new RequestInputException("Subscription not found");
                if (subscription.UserId != UserId)
                    throw new RequestInputException("User not owner of subscription");
            }
        }
        public sealed record Result();
        #endregion
    }
}
