using MemCheck.Database;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Immutable;
using MemCheck.Domain;

namespace MemCheck.Application.Notifying
{
    public sealed class Notifier
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        private readonly UserCardVersionsNotifier userCardVersionsNotifier;
        private readonly UserCardDeletionsNotifier userCardDeletionsNotifier;
        public const int MaxLengthForTextFields = 150;
        #endregion
        public Notifier(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
            userCardVersionsNotifier = new UserCardVersionsNotifier(dbContext);
            userCardDeletionsNotifier = new UserCardDeletionsNotifier(dbContext);
        }
        private async Task<UserNotifications> GetUserNotificationsAsync(MemCheckUser user)
        {
            var registeredCardCount = await dbContext.CardNotifications.Where(notif => notif.UserId == user.Id).CountAsync();
            var cardVersions = userCardVersionsNotifier.Run(user);
            var cardDeletions = userCardDeletionsNotifier.Run(user);

            //var endOfRequest = DateTime.UtcNow;
            //foreach (var registeredCard in registeredCardsForUser.Values)
            //    registeredCard.LastNotificationUtcDate = endOfRequest;

            await dbContext.SaveChangesAsync();

            return new UserNotifications(
                user.UserName,
                user.Email,
                registeredCardCount,
                cardVersions,
                cardDeletions
                );
        }
        public async Task<NotifierResult> GetNotificationsAsync()
        {
            var users = new UsersToNotifyGetter(dbContext).Run();
            var userNotifications = new List<UserNotifications>();
            foreach (var user in users)
                userNotifications.Add(await GetUserNotificationsAsync(user));
            return new NotifierResult(userNotifications);
        }
        #region Result classes
        public class NotifierResult
        {
            public NotifierResult(IEnumerable<UserNotifications> userNotifications)
            {
                UserNotifications = userNotifications;
            }
            public IEnumerable<UserNotifications> UserNotifications { get; }
        }
        public class UserNotifications
        {
            public UserNotifications(string userName, string userEmail, int registeredCardCount, IEnumerable<CardVersion> cardVersions, IEnumerable<CardDeletion> deletedCards)
            {
                UserName = userName;
                UserEmail = userEmail;
                RegisteredCardCount = registeredCardCount;
                CardVersions = cardVersions.ToImmutableArray();
                DeletedCards = deletedCards.ToImmutableArray();
            }
            public string UserName { get; }
            public string UserEmail { get; }
            public int RegisteredCardCount { get; }
            public ImmutableArray<CardVersion> CardVersions { get; }
            public ImmutableArray<CardDeletion> DeletedCards { get; }
        }
        #endregion
    }
}