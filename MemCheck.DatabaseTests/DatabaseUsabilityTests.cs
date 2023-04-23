using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace MemCheck.Database;

/* Checks that the database specified in the `appsettings.json` file is ok.
 * Note that when running in GitHub actions, the settings in the file are substituted with the real production database.
 */
[TestClass(), TestCategory("IntegrationTests")]
public sealed class DatabaseUsabilityTests : IDisposable
{
    #region Fields
    private readonly DbContext dbContext;
    #endregion
    #region Private method
    private static IConfigurationRoot GetIConfigurationRoot()
    {
        var basePath = Path.GetDirectoryName(typeof(DatabaseUsabilityTests).Assembly.Location)!;
        return new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();
    }
    private static IMigrationsAssembly GetMigrationsAssembly()
    {
        using var dbContext = new MemCheckDbContext(new DbContextOptionsBuilder<MemCheckDbContext>().UseSqlServer(@"Server=none;Database=none;Trusted_Connection=True;").Options);
        var result = dbContext.GetService<IMigrationsAssembly>();
        Assert.IsNotNull(result);
        return result;
    }
    private static IRelationalModel GetModelFromMigrations()
    {
        var migrationsAssembly = GetMigrationsAssembly();
        var migrationsAssemblyModel = migrationsAssembly.ModelSnapshot?.Model!;
        if (migrationsAssemblyModel is IMutableModel mutableModel)
            migrationsAssemblyModel = mutableModel.FinalizeModel();
        using var dbContext = new MemCheckDbContext(new DbContextOptionsBuilder<MemCheckDbContext>().UseSqlServer(@"Server=none;Database=none;Trusted_Connection=True;").Options);
        migrationsAssemblyModel = dbContext.GetService<IModelRuntimeInitializer>().Initialize(migrationsAssemblyModel);
        var result = migrationsAssemblyModel.GetRelationalModel();
        return result;
    }
    #endregion
    public DatabaseUsabilityTests()
    {
        var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING_FOR_DB_USABILITY_CHECK");
        string source;

        if (!string.IsNullOrEmpty(connectionString))
            source = "environment variable";
        else
        {
            connectionString = GetIConfigurationRoot()[$"ConnectionStrings:Connection"];
            source = "config file";
        }

        dbContext = new MemCheckDbContext(new DbContextOptionsBuilder<MemCheckDbContext>().UseSqlServer(connectionString).Options);
        Assert.IsTrue(dbContext.Database.CanConnect(), $"Unable to connect to DB with infro from {source}");
    }
    [TestMethod()]
    public void TestNoMigrationNeeded()
    {
        var modelFromMigrations = GetModelFromMigrations();
        var modelFromCode = dbContext.GetService<IDesignTimeModel>().Model.GetRelationalModel();

        var changes = dbContext.GetService<IMigrationsModelDiffer>().GetDifferences(modelFromMigrations, modelFromCode);
        Assert.AreEqual(0, changes.Count, "A DB model update is needed - Run dotnet ef migrations add - Changes: " + string.Join(',', changes.Select(change => change.ToString())));
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
        var migrationsAssembly = GetMigrationsAssembly();
        var expectedLastMigrationName = migrationsAssembly.Migrations.Last().Key;
        var actual = dbContext.Database.GetAppliedMigrations().Last();
        Assert.AreEqual(expectedLastMigrationName, actual);
    }
    [TestMethod()]
    public void TestNoDbUpdateNeeded()
    {
        var pendingDbUpdates = dbContext.Database.GetPendingMigrations();
        Assert.AreEqual(0, pendingDbUpdates.Count(), $"There are {pendingDbUpdates.Count()} migrations to run on the DB: " + string.Join(',', pendingDbUpdates.Select(pendingDbUpdate => pendingDbUpdate.ToString())));
    }
    public void Dispose()
    {
        dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
