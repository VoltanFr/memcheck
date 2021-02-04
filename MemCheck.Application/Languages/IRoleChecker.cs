using MemCheck.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MemCheck.Application.Languages
{
    public interface IRoleChecker
    {
        const string AdminRoleName = "Admin";

        Task<bool> UserIsAdminAsync(MemCheckUser user);
        Task<IEnumerable<string>> GetRolesAsync(MemCheckUser user);
    }
}
