using MemCheck.Database;
using System.Threading.Tasks;

namespace MemCheck.CommandLineDbClient
{
    internal interface IMemCheckTest
    {
        Task RunAsync(MemCheckDbContext dbContext);
        void DescribeForOpportunityToCancel();
    }
}
