using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using System.Data.SqlClient;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MemCheck.Application.Helpers;

public static class DbHelper
{
    public static DbContextOptions<MemCheckDbContext> GetEmptyTestDB([CallerFilePath] string callerFilePath = "")
    {
        var attempts = 0;
        while (true)
        {
            try
            {
                var name = Path.GetFileNameWithoutExtension(callerFilePath);
                var connectionString = @$"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog={name};Integrated Security=True;Connect Timeout=120;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
                var result = new DbContextOptionsBuilder<MemCheckDbContext>().UseSqlServer(connectionString).Options;
                using (var dbContext = new MemCheckDbContext(result))
                {
                    dbContext.Database.EnsureDeleted();
                    dbContext.Database.EnsureCreated();
                }
                return result;
            }
            catch (SqlException e)
            {
                if (!e.Message.Contains("Connection Timeout Expired") || attempts > 5)  //Happens sometimes in GitHub actions
                    throw;
                Thread.Sleep((attempts + 1) * 2000);
            }
        }
    }
}
