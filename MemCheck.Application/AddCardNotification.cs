using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application
{
    public sealed class AddCardNotification
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public AddCardNotification(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task RunAsync(Request request)
        {
            request.CheckValidity();
            if (dbContext.CardNotifications.Where(notif => notif.UserId == request.User.Id && notif.CardId == request.CardId).Any())
                return;

            dbContext.CardNotifications.Add(new CardNotification() { UserId = request.User.Id, CardId = request.CardId, RegistrationUtcDate = DateTime.UtcNow, LastNotificationUtcDate = DateTime.MinValue, RegistrationMethod = CardNotification.CardNotificationRegistrationMethod_ExplicitByUser });
            await dbContext.SaveChangesAsync();
        }
        #region Request class
        public sealed class Request
        {
            public Request(MemCheckUser user, Guid cardId)
            {
                User = user;
                CardId = cardId;
            }
            public MemCheckUser User { get; }
            public Guid CardId { get; }
            public void CheckValidity()
            {
                if (QueryValidationHelper.IsReservedGuid(User.Id))
                    throw new RequestInputException("Invalid user id");
                if (QueryValidationHelper.IsReservedGuid(CardId))
                    throw new RequestInputException("Invalid card id");
            }
        }
        #endregion
    }
}