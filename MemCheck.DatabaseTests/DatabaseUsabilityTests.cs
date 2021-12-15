using MemCheck.Database;
using MemCheck.Database.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MemCheck.DatabaseTests
{
    /* Checks that the database specified in the `appsettings.json` file is ok.
     * Note that when running in GitHub actions, the settings in the file are substituted with the real production database.
     */
    [TestClass(), TestCategory("IntegrationTests")]
    public class DatabaseUsabilityTests : IDisposable
    {
        #region Fields
        private readonly DbContext dbContext;
        #endregion
        #region Private method
        private static IConfigurationRoot GetIConfigurationRoot()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(typeof(DatabaseUsabilityTests).Assembly.Location))
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
        }
        #endregion
        public DatabaseUsabilityTests()
        {
            var connectionString = GetIConfigurationRoot()[$"ConnectionStrings:Connection"];
            dbContext = new MemCheckDbContext(new DbContextOptionsBuilder<MemCheckDbContext>().UseSqlServer(connectionString).Options);
        }
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
        public void TestConnectionToRightDB()
        {
            //If the DB does not exist, this code will consider all the migrations to run on an empty DB

            var appliedMigrations = dbContext.Database.GetAppliedMigrations();
            Assert.AreNotEqual(0, appliedMigrations.Count(), "The DB has had no migration at all, it is not the expected DB");
        }
        [TestMethod()]
        public void TestLastMigrationName()
        {
            var expectedLastMigration = typeof(AddCascadeBehaviorForUserCardRating);

            var expectedLastMigrationName = ((MigrationAttribute)(expectedLastMigration.GetCustomAttribute(typeof(MigrationAttribute)))!).Id;
            var actual = dbContext.Database.GetAppliedMigrations().Last();

            Assert.AreEqual(expectedLastMigrationName, actual);
        }
        [TestMethod()]
        public void TestNoDbUpdateNeeded()
        {
            var pendingDbUpdates = dbContext.Database.GetPendingMigrations();
            Assert.AreEqual(0, pendingDbUpdates.Count(), $"There are {pendingDbUpdates.Count()} migrations to run on the DB");
        }
        public void Dispose()
        {
            dbContext.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
