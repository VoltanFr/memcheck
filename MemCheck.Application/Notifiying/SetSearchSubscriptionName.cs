using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Notifying
{
    public sealed class SetSearchSubscriptionName
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public SetSearchSubscriptionName(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task RunAsync(Request request)
        {
            await request.CheckValidityAsync(dbContext);
            var subscription = await dbContext.SearchSubscriptions.Where(s => s.Id == request.SubscriptionId).SingleAsync();
            subscription.Name = request.Name;
            await dbContext.SaveChangesAsync();
        }
        internal static void CreateSubscription(MemCheckDbContext dbContext, Guid userId, Guid cardId, DateTime registrationUtcDate, int registrationMethod)
        {
            if (dbContext.CardNotifications.Where(notif => notif.UserId == userId && notif.CardId == cardId).Any())
                return;
            CardNotificationSubscription notif = new CardNotificationSubscription
            {
                CardId = cardId,
                UserId = userId,
                RegistrationUtcDate = registrationUtcDate,
                RegistrationMethod = registrationMethod,
                LastNotificationUtcDate = registrationUtcDate
            };
            dbContext.CardNotifications.Add(notif);
        }
        #region Request class
        public sealed class Request
        {
            public const int MinNameLength = 3;
            public const int MaxNameLength = 36;
            public Request(Guid userId, Guid subscriptionId, string name)
            {
                UserId = userId;
                SubscriptionId = subscriptionId;
                Name = name.Trim();
            }
            public Guid UserId { get; }
            public Guid SubscriptionId { get; }
            public string Name { get; }
            public async Task CheckValidityAsync(MemCheckDbContext dbContext)
            {
                QueryValidationHelper.CheckNotReservedGuid(UserId);
                QueryValidationHelper.CheckNotReservedGuid(SubscriptionId);
                if (Name.Length < MinNameLength)
                    throw new RequestInputException($"Name '{Name}' is too short, must be between {MinNameLength} and {MaxNameLength} chars long, is {Name.Length}");
                if (Name.Length > MaxNameLength)
                    throw new RequestInputException($"Name '{Name}' is too long, must be between {MinNameLength} and {MaxNameLength} chars long, is {Name.Length}");
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
