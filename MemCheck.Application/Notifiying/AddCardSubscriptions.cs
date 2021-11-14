using MemCheck.Application.QueryValidation;
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
        private readonly CallContext callContext;
        #endregion
        public AddCardSubscriptions(CallContext callContext)
        {
            this.callContext = callContext;
        }
        public async Task RunAsync(Request request)
        {
            request.CheckValidity(callContext.DbContext);

            var now = DateTime.UtcNow;

            foreach (var cardId in request.CardIds)
                CreateSubscription(callContext.DbContext, request.UserId, cardId, now, CardNotificationSubscription.CardNotificationRegistrationMethod_ExplicitByUser);

            await callContext.DbContext.SaveChangesAsync();
            callContext.TelemetryClient.TrackEvent("AddCardSubscriptions", ("CardCount", request.CardIds.Count().ToString()));
        }
        internal static void CreateSubscription(MemCheckDbContext dbContext, Guid userId, Guid cardId, DateTime registrationUtcDate, int registrationMethod)
        {
            if (dbContext.CardNotifications.Where(notif => notif.UserId == userId && notif.CardId == cardId).Any())
                return;
            CardNotificationSubscription notif = new()
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
            public void CheckValidity(MemCheckDbContext dbContext)
            {
                QueryValidationHelper.CheckNotReservedGuid(UserId);
                if (CardIds.Any(cardId => QueryValidationHelper.IsReservedGuid(cardId)))
                    throw new RequestInputException($"Invalid card id");
                foreach (var cardId in CardIds)
                    CardVisibilityHelper.CheckUserIsAllowedToViewCards(dbContext, UserId, cardId);
            }
        }
        #endregion
    }
}
