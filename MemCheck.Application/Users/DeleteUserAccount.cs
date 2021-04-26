using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MemCheck.Application.Users
{
    //Use this when implementing the real user account deletion by himself:                 await signInManager.SignOutAsync();
    public sealed class DeleteUserAccount
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        private readonly IRoleChecker roleChecker;
        public const string DeletedUserName = "!DeletedUserName!";
        public const string DeletedUserEmail = "!DeletedUserEmail!";
        #endregion
        public DeleteUserAccount(MemCheckDbContext dbContext, IRoleChecker roleChecker)
        {
            this.dbContext = dbContext;
            this.roleChecker = roleChecker;
        }
        public async Task RunAsync(Request request)
        {
            await request.CheckValidityAsync(dbContext, roleChecker);
            var userToDelete = await dbContext.Users.SingleAsync(user => user.Id == request.UserToDeleteId);
            userToDelete.UserName = DeletedUserName;
            userToDelete.Email = DeletedUserEmail;
            userToDelete.EmailConfirmed = false;
            userToDelete.LockoutEnabled = true;
            userToDelete.LockoutEnd = DateTime.MaxValue;
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
