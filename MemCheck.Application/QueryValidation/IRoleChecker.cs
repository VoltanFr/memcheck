using MemCheck.Domain;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MemCheck.Application.QueryValidation
{
    public interface IRoleChecker
    {
        const string AdminRoleName = "Admin";

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
    }
}
