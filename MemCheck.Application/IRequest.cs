using System.Threading.Tasks;

namespace MemCheck.Application
{
    public interface IRequest
    {
        Task CheckValidityAsync(CallContext callContext);
    }

}
