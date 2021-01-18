using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Runtime.CompilerServices;

namespace MemCheck.Application.Tests.Helpers
{
    public static class DbHelper
    {
        public static DbContextOptions<MemCheckDbContext> GetEmptyTestDB([CallerFilePath] string callerFilePath = "")
        {
            var name = Path.GetFileNameWithoutExtension(callerFilePath);
            var connectionString = @$"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog={name};Integrated Security=True;Connect Timeout=60;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
            var result = new DbContextOptionsBuilder<MemCheckDbContext>().UseSqlServer(connectionString).Options;
            using (var dbContext = new MemCheckDbContext(result))
            {
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();
            }
            return result;
        }
    }
}