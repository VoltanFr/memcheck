using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace MemCheck.DatabaseTests
{
    [TestClass()]
    public class ModelTests
    {
        [TestMethod()]
        public void TestNoPendingChanges()
        {
            using var dbContext = new MemCheckDbContext(new DbContextOptionsBuilder<MemCheckDbContext>().UseSqlServer(@"Server=none;Database=none;Trusted_Connection=True;").Options);

            var relationalDependencies = dbContext.GetService<RelationalConventionSetBuilderDependencies>();
            var relationalModelConvention = new RelationalModelConvention(dbContext.GetService<ProviderConventionSetBuilderDependencies>(), relationalDependencies);
            var snapshotModel = (IConventionModel)dbContext.GetService<IMigrationsAssembly>().ModelSnapshot.Model;
            var finalizedSnapshotModel = relationalModelConvention.ProcessModelFinalized(snapshotModel).GetRelationalModel();

            var possiblyModifiedModel = dbContext.Model.GetRelationalModel();

            var changes = dbContext.GetService<IMigrationsModelDiffer>().GetDifferences(finalizedSnapshotModel, possiblyModifiedModel);
            Assert.AreEqual(0, changes.Count, "A DB model update is needed - Run dotnet ef migrations add - " + string.Join(',', changes.Select(change => change.ToString())));
        }
    }
}