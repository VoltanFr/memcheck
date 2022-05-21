using MemCheck.Application.QueryValidation;
using MemCheck.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Users;

/// <summary>
/// We don't really delete a user account, we just mark it as deleted and anonymize it.
/// Anonymize means that we replace the user name with DeleteUserAccount.DeletedUserName.
/// Note that we don't replace the NormalizedUserName, so that the user name may not be reused (I prefer to forbid the reusing of a user name because I think it would be error prone for users: one could think another user is a person he is not).
/// This is because the user id is used in many places, such as card version creator, image uploader, or tag creator.
/// </summary>
public sealed class DeleteUserAccount : RequestRunner<DeleteUserAccount.Request, DeleteUserAccount.Result>
{
    #region Fields
    private readonly UserManager<MemCheckUser> userManager;
    private readonly DateTime? runDate;
    public const string DeletedUserName = "!DeletedUserName!";
    public const string DeletedUserEmail = "!DeletedUserEmail!";
    #endregion
    #region Private methods
    private void DeleteDecks(Guid userToDeleteId)
    {
        var decks = DbContext.Decks.Where(d => d.Owner.Id == userToDeleteId);
        DbContext.Decks.RemoveRange(decks);
    }
    private async Task AnonymizeUser(Guid userToDeleteId)
    {
        var userToDelete = await DbContext.Users.SingleAsync(user => user.Id == userToDeleteId);
        var passwordRemovalResult = await userManager.RemovePasswordAsync(userToDelete);
        if (!passwordRemovalResult.Succeeded)
            throw new RequestRunException($"Failed to remove password of user '{userToDelete.UserName}' - {string.Join('-', passwordRemovalResult.Errors.Select(err => err.Description))}");
        userToDelete.UserName = DeletedUserName;
        userToDelete.Email = DeletedUserEmail;
        userToDelete.EmailConfirmed = false;
        userToDelete.LockoutEnabled = true;
        userToDelete.LockoutEnd = DateTime.MaxValue;
        userToDelete.DeletionDate = runDate ?? DateTime.UtcNow;
    }
    private void DeleteRatings(Guid userToDeleteId)
    {
        var ratings = DbContext.UserCardRatings.Where(rating => rating.UserId == userToDeleteId);
        DbContext.UserCardRatings.RemoveRange(ratings);
    }
    private void DeleteSearchSubscriptions(Guid userToDeleteId)
    {
        var subscriptions = DbContext.SearchSubscriptions.Where(subscription => subscription.UserId == userToDeleteId);
        DbContext.SearchSubscriptions.RemoveRange(subscriptions);
    }
    private async Task DeletePrivateCardsAsync(Guid userToDeleteId)
    {
        //We delete the user's private cards, with all previous versions, without considerations of what happened in the history of the card
        //We completely ignore non-private cards, including previous versions which were private (this could be subject to debate)
        var privateCards = DbContext.Cards.Where(card => card.UsersWithView.Count() == 1 && card.UsersWithView.Any(userWithView => userWithView.UserId == userToDeleteId));
        var privateCardIds = await privateCards.Select(card => card.Id).ToListAsync();
        var previousVersions = DbContext.CardPreviousVersions.Where(previous => privateCardIds.Contains(previous.Card));
        DbContext.CardPreviousVersions.RemoveRange(previousVersions);
        DbContext.Cards.RemoveRange(privateCards);
    }
    private void UpdateCardsVisibility(Guid userToDeleteId)
    {
        //I don't care about deleted cards (table UsersWithViewOnCardPreviousVersions), because I think it won't be a problem. This can be debated.
        var usersWithViewOnCards = DbContext.UsersWithViewOnCards.Where(user => user.UserId == userToDeleteId);
        DbContext.UsersWithViewOnCards.RemoveRange(usersWithViewOnCards);
    }
    private void DeleteCardNotificationSubscriptions(Guid userToDeleteId)
    {
        var subscriptions = DbContext.CardNotifications.Where(subscription => subscription.UserId == userToDeleteId);
        DbContext.CardNotifications.RemoveRange(subscriptions);
    }
    #endregion
    public DeleteUserAccount(CallContext callContext, UserManager<MemCheckUser> userManager, DateTime? runDate = null) : base(callContext)
    {
        this.userManager = userManager;
        this.runDate = runDate;
    }
    protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
    {
        DeleteDecks(request.UserToDeleteId);
        DeleteRatings(request.UserToDeleteId);
        DeleteSearchSubscriptions(request.UserToDeleteId);
        await DeletePrivateCardsAsync(request.UserToDeleteId);
        UpdateCardsVisibility(request.UserToDeleteId);
        DeleteCardNotificationSubscriptions(request.UserToDeleteId);
        await AnonymizeUser(request.UserToDeleteId);
        await DbContext.SaveChangesAsync();
        return new ResultWithMetrologyProperties<Result>(new Result(), ("LoggedUserId", request.LoggedUserId.ToString()));
    }
    #region Request & Result
    public sealed record Request(Guid LoggedUserId, Guid UserToDeleteId) : IRequest
    {
        public async Task CheckValidityAsync(CallContext callContext)
        {
            if (LoggedUserId != UserToDeleteId)
                await QueryValidationHelper.CheckUserExistsAndIsAdminAsync(callContext.DbContext, LoggedUserId, callContext.RoleChecker);

            await QueryValidationHelper.CheckUserExistsAsync(callContext.DbContext, UserToDeleteId);
            var userToDelete = await callContext.DbContext.Users.AsNoTracking().SingleAsync(user => user.Id == UserToDeleteId);
            if (await callContext.RoleChecker.UserIsAdminAsync(userToDelete))
                //Additional security: forbid deleting an admin account
                throw new InvalidOperationException("User to delete is admin");
        }
    }
    public sealed record Result();
    #endregion
}
