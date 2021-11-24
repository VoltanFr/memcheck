using MemCheck.Database;
using System.Threading.Tasks;

namespace MemCheck.Application
{
    public interface IRequest
    {
        Task CheckValidityAsync(MemCheckDbContext dbContext);
    }

}
