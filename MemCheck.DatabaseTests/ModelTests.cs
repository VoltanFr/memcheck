using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MemCheck.DatabaseTests
{
    [TestClass()]
    public class ModelTests
    {
        #region Private method
        private static IConfigurationRoot GetIConfigurationRoot()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(typeof(ModelTests).Assembly.Location))
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
        }
        #endregion
        [TestMethod()]
        public void TestNoDbMigrationNeeded()
        {
            using var dbContext = new MemCheckDbContext(new DbContextOptionsBuilder<MemCheckDbContext>().UseSqlServer(@"Server=none;Database=none;Trusted_Connection=True;").Options);

            var relationalDependencies = dbContext.GetService<RelationalConventionSetBuilderDependencies>();
            var relationalModelConvention = new RelationalModelConvention(dbContext.GetService<ProviderConventionSetBuilderDependencies>(), relationalDependencies);
            var modelSnapshot = (IConventionModel)dbContext.GetService<IMigrationsAssembly>().ModelSnapshot.Model;
            var finalizedSnapshotModel = relationalModelConvention.ProcessModelFinalized(modelSnapshot).GetRelationalModel();

            var possiblyModifiedModel = dbContext.Model.GetRelationalModel();

            var changes = dbContext.GetService<IMigrationsModelDiffer>().GetDifferences(finalizedSnapshotModel, possiblyModifiedModel);
            Assert.AreEqual(0, changes.Count, "A DB model update is needed - Run dotnet ef migrations add - " + string.Join(',', changes.Select(change => change.ToString())));
        }
        [TestMethod()]
        public void TestNoDbUpdateNeededCore()
        {
            var connectionString = GetIConfigurationRoot()[$"ConnectionStrings:Connection"];

            using var dbContext = new MemCheckDbContext(new DbContextOptionsBuilder<MemCheckDbContext>().UseSqlServer(connectionString).Options);

            var pendingDbUpdates = dbContext.Database.GetPendingMigrations();
            Assert.AreEqual(0, pendingDbUpdates.Count());
        }
    }
}