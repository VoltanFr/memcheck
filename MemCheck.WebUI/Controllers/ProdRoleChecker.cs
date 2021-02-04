using MemCheck.Application.Languages;
using MemCheck.Domain;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MemCheck.WebUI.Controllers
{
    public class ProdRoleChecker : IRoleChecker
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
