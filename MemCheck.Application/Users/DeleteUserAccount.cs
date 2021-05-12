using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Users
{
    //Use this when implementing the real user account deletion by himself:                 await signInManager.SignOutAsync();
    /// <summary>
    /// We don't really delete a user account, we just mark it as deleted and anonymize it.
    /// This is because the user id is used in many places, such as card version creator, image uploader, or tag creator.
    /// </summary>
    public sealed class DeleteUserAccount
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        private readonly IRoleChecker roleChecker;
        private readonly UserManager<MemCheckUser> userManager;
        public const string DeletedUserName = "!DeletedUserName!";
        public const string DeletedUserEmail = "!DeletedUserEmail!";
        #endregion
        #region Private methods
        private void DeleteDecks(Guid userToDeleteId)
        {
            var decks = dbContext.Decks.Where(d => d.Owner.Id == userToDeleteId);
            dbContext.Decks.RemoveRange(decks);
        }
        private async Task AnonymizeUser(Guid userToDeleteId, DateTime? runUtcDate)
        {
            var userToDelete = await dbContext.Users.SingleAsync(user => user.Id == userToDeleteId);
            userToDelete.UserName = DeletedUserName;
            userToDelete.Email = DeletedUserEmail;
            userToDelete.EmailConfirmed = false;
            userToDelete.LockoutEnabled = true;
            userToDelete.LockoutEnd = DateTime.MaxValue;
            userToDelete.DeletionDate = runUtcDate ?? DateTime.UtcNow;
            await userManager.RemovePasswordAsync(userToDelete);
        }
        private void DeleteRatings(Guid userToDeleteId)
        {
            var ratings = dbContext.UserCardRatings.Where(rating => rating.UserId == userToDeleteId);
            dbContext.UserCardRatings.RemoveRange(ratings);
        }
        private void DeleteSearchSubscriptions(Guid userToDeleteId)
        {
            var subscriptions = dbContext.SearchSubscriptions.Where(subscription => subscription.UserId == userToDeleteId);
            dbContext.SearchSubscriptions.RemoveRange(subscriptions);
        }
        private async Task DeletePrivateCardsAsync(Guid userToDeleteId)
        {
            //We delete the user's private cards, with all previous versions, without considerations of what happened in the history of the card
            //We completely ignore non-private cards, including previous versions which were private (this could be subject to debate)
            var privateCards = dbContext.Cards.Where(card => card.UsersWithView.Count() == 1 && card.UsersWithView.Any(userWithView => userWithView.UserId == userToDeleteId));
            var privateCardIds = await privateCards.Select(card => card.Id).ToListAsync();
            var previousVersions = dbContext.CardPreviousVersions.Where(previous => privateCardIds.Contains(previous.Card));
            dbContext.CardPreviousVersions.RemoveRange(previousVersions);
            dbContext.Cards.RemoveRange(privateCards);
        }
        private void UpdateCardsVisibility(Guid userToDeleteId)
        {
            //I don't care about deleted cards (table UsersWithViewOnCardPreviousVersions), because I think it won't be a problem. This can be debated.
            var usersWithViewOnCards = dbContext.UsersWithViewOnCards.Where(user => user.UserId == userToDeleteId);
            dbContext.UsersWithViewOnCards.RemoveRange(usersWithViewOnCards);
        }
        private void DeleteCardNotificationSubscriptions(Guid userToDeleteId)
        {
            var subscriptions = dbContext.CardNotifications.Where(subscription => subscription.UserId == userToDeleteId);
            dbContext.CardNotifications.RemoveRange(subscriptions);
        }
        #endregion
        public DeleteUserAccount(MemCheckDbContext dbContext, IRoleChecker roleChecker, UserManager<MemCheckUser> userManager)
        {
            this.dbContext = dbContext;
            this.roleChecker = roleChecker;
            this.userManager = userManager;
        }
        public async Task RunAsync(Request request, DateTime? runUtcDate = null)
        {
            await request.CheckValidityAsync(dbContext, roleChecker);
            DeleteDecks(request.UserToDeleteId);
            DeleteRatings(request.UserToDeleteId);
            DeleteSearchSubscriptions(request.UserToDeleteId);
            await DeletePrivateCardsAsync(request.UserToDeleteId);
            UpdateCardsVisibility(request.UserToDeleteId);
            DeleteCardNotificationSubscriptions(request.UserToDeleteId);
            await AnonymizeUser(request.UserToDeleteId, runUtcDate);
            await dbContext.SaveChangesAsync();
        }
        #region Request
        public sealed record Request(Guid LoggedUserId, Guid UserToDeleteId)
        {
            public async Task CheckValidityAsync(MemCheckDbContext dbContext, IRoleChecker roleChecker)
            {
                await QueryValidationHelper.CheckUserExistsAsync(dbContext, LoggedUserId);
                var loggedUser = await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == LoggedUserId);
                if (!await roleChecker.UserIsAdminAsync(loggedUser))
                    //Additional security, to be modified when we really want to support account deletion by user himself
                    throw new InvalidOperationException("Logged user is not admin");

                await QueryValidationHelper.CheckUserExistsAsync(dbContext, UserToDeleteId);
                var userToDelete = await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == UserToDeleteId);
                if (await roleChecker.UserIsAdminAsync(userToDelete))
                    //Additional security: forbid deleting an admin account
                    throw new InvalidOperationException("User to delete is admin");
            }
        }
        #endregion
    }
}
