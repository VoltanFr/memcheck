using MemCheck.Domain;
using System.Threading.Tasks;

namespace MemCheck.Application.Languages
{
    public interface IRoleChecker
    {
        Task<bool> UserIsAdminAsync(MemCheckUser user);
    }
}
