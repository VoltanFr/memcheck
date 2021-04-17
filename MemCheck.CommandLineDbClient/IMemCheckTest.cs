using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace MemCheck.CommandLineDbClient
{
    internal interface IMemCheckTest
    {
        Task RunAsync();
        void DescribeForOpportunityToCancel();
    }
    public sealed class PrimaryDbContext : MemCheckDbContext
    {
        public PrimaryDbContext(DbContextOptions<PrimaryDbContext> options) : base(options)
        {
        }
    }
    public sealed class SecondaryDbContext : MemCheckDbContext
    {
        public SecondaryDbContext(DbContextOptions<SecondaryDbContext> options) : base(options)
        {
        }
    }
}
