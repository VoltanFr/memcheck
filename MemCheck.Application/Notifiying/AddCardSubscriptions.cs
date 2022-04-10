using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Notifiying
{
    public sealed class AddCardSubscriptions : RequestRunner<AddCardSubscriptions.Request, AddCardSubscriptions.Result>
    {
        public AddCardSubscriptions(CallContext callContext) : base(callContext)
        {
        }
        protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
        {
            var now = DateTime.UtcNow;

            foreach (var cardId in request.CardIds)
                CreateSubscription(DbContext, request.UserId, cardId, now, CardNotificationSubscription.CardNotificationRegistrationMethodExplicitByUser);

            await DbContext.SaveChangesAsync();
            return new ResultWithMetrologyProperties<Result>(new Result(), IntMetric("CardCount", request.CardIds.Count()));
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
                QueryValidationHelper.CheckNotReservedGuid(UserId);
                if (CardIds.Any(cardId => QueryValidationHelper.IsReservedGuid(cardId)))
                    throw new RequestInputException($"Invalid card id");
                foreach (var cardId in CardIds)
                    CardVisibilityHelper.CheckUserIsAllowedToViewCard(callContext.DbContext, UserId, cardId);
                await Task.CompletedTask;
            }
        }
        public sealed record Result();
        #endregion
    }
}
