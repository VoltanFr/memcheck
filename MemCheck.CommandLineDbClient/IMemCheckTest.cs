using System.Threading.Tasks;

namespace MemCheck.CommandLineDbClient
{
    internal interface IMemCheckTest
    {
        Task RunAsync();
        void DescribeForOpportunityToCancel();
    }
}
