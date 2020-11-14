using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application
{
    public sealed class RemoveCardNotification
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public RemoveCardNotification(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task RunAsync(Request request)
        {
            request.CheckValidity();
            var existing = await dbContext.CardNotifications.Where(notif => notif.UserId == request.UserId && notif.CardId == request.CardId).SingleOrDefaultAsync();

            if (existing == null)
                return;

            dbContext.CardNotifications.Remove(existing);
            await dbContext.SaveChangesAsync();
        }
        #region Request class
        public sealed class Request
        {
            public Request(Guid userId, Guid cardId)
            {
                UserId = userId;
                CardId = cardId;
            }
            public Guid UserId { get; }
            public Guid CardId { get; }
            public void CheckValidity()
            {
                if (QueryValidationHelper.IsReservedGuid(UserId))
                    throw new RequestInputException("Invalid user id");
                if (QueryValidationHelper.IsReservedGuid(CardId))
                    throw new RequestInputException("Invalid card id");
            }
        }
        #endregion
    }
}