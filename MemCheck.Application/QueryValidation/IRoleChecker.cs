using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.QueryValidation;

public interface IRoleChecker
{
    const string AdminRoleName = "Admin";

    Task<bool> UserIsAdminAsync(MemCheckDbContext dbContext, Guid userId);
    Task<bool> UserIsAdminAsync(MemCheckUser user);
    Task<IEnumerable<string>> GetRolesAsync(MemCheckUser user);
}

public sealed class ProdRoleChecker : IRoleChecker
{
    #region Fields
    private readonly UserManager<MemCheckUser> userManager;
    #endregion
    public ProdRoleChecker(UserManager<MemCheckUser> userManager)
    {
        this.userManager = userManager;
    }
    public async Task<IEnumerable<string>> GetRolesAsync(MemCheckUser user)
    {
        return await userManager.GetRolesAsync(user);
    }
    public async Task<bool> UserIsAdminAsync(MemCheckUser user)
    {
        return await userManager.IsInRoleAsync(user, IRoleChecker.AdminRoleName);
    }
    public async Task<bool> UserIsAdminAsync(MemCheckDbContext dbContext, Guid userId)
    {
        var user = await dbContext.Users.AsNoTracking().Where(user => user.Id == userId).SingleAsync();
        return await UserIsAdminAsync(user);
    }
}
