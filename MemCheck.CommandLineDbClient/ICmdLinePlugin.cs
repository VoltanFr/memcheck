using System.Threading.Tasks;

namespace MemCheck.CommandLineDbClient
{
    internal interface ICmdLinePlugin
    {
        Task RunAsync();
        void DescribeForOpportunityToCancel();
    }
}
