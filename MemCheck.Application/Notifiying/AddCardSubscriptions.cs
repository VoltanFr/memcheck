using MemCheck.Database;
using MemCheck.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Notifying
{
    public sealed class AddCardSubscriptions
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public AddCardSubscriptions(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task RunAsync(Request request)
        {
            await request.CheckValidityAsync(dbContext);

            var now = DateTime.UtcNow;

            foreach (var cardId in request.CardIds)
                CreateSubscription(dbContext, request.UserId, cardId, now, CardNotificationSubscription.CardNotificationRegistrationMethod_ExplicitByUser);

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
            public Request(Guid userId, IEnumerable<Guid> cardIds)
            {
                UserId = userId;
                CardIds = cardIds;
            }
            public Guid UserId { get; }
            public IEnumerable<Guid> CardIds { get; }
            public async Task CheckValidityAsync(MemCheckDbContext dbContext)
            {
                if (QueryValidationHelper.IsReservedGuid(UserId))
                    throw new RequestInputException("Invalid user id");
                if (CardIds.Any(cardId => QueryValidationHelper.IsReservedGuid(cardId)))
                    throw new RequestInputException($"Invalid card id");
                foreach (var cardId in CardIds)
                    await QueryValidationHelper.CheckUserIsAllowedToViewCardAsync(dbContext, UserId, cardId);
            }
        }
        #endregion
    }
}
