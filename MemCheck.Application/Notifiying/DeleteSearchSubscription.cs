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
        private readonly MemCheckDbContext dbContext;
        #endregion
        public DeleteSearchSubscription(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task RunAsync(Request request)
        {
            await request.CheckValidityAsync(dbContext);
            var subscription = await dbContext.SearchSubscriptions.Where(s => s.Id == request.SubscriptionId).SingleAsync();
            dbContext.SearchSubscriptions.Remove(subscription);
            await dbContext.SaveChangesAsync();
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
