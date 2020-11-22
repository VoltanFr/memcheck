using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application
{
    public sealed class AddCardNotifications
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public AddCardNotifications(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task RunAsync(Request request)
        {
            request.CheckValidity();

            foreach (var cardId in request.CardIds)
            {
                if (!dbContext.CardNotifications.Where(notif => notif.UserId == request.UserId && notif.CardId == cardId).Any())
                {
                    CardNotificationSubscription notif = new CardNotificationSubscription
                    {
                        CardId = cardId,
                        UserId = request.UserId,
                        RegistrationUtcDate = DateTime.UtcNow,
                        RegistrationMethod = CardNotificationSubscription.CardNotificationRegistrationMethod_ExplicitByUser,
                        LastNotificationUtcDate = DateTime.UtcNow
                    };
                    dbContext.CardNotifications.Add(notif);
                }
            }

            await dbContext.SaveChangesAsync();
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
