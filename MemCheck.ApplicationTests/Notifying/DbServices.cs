using Microsoft.EntityFrameworkCore;
using MemCheck.Database;
using System;

namespace MemCheck.Application.Tests.Notifying
{
    public static class DbServices
    {
        public static DbContextOptions<MemCheckDbContext> GetEmptyTestDB(Type testClass)
        {
            var connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=UserCardDeletionsNotifierTests;Integrated Security=True;Connect Timeout=60;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
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