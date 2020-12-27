using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using MemCheck.Database;
using MemCheck.Application.Tests;
using System.Linq;
using MemCheck.Application.Tests.Helpers;
using MemCheck.Application.QueryValidation;

namespace MemCheck.Application.Loading
{
    [TestClass()]
    public class GetDecksWithLearnCountsTests
    {
        [TestMethod()]
        public async Task TestEmptyDB_UserNotLoggedIn()
        {
            using (var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB()))
                await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new GetDecksWithLearnCounts(dbContext).RunAsync(new GetDecksWithLearnCounts.Request(Guid.Empty)));
        }
        [TestMethod()]
        public async Task TestEmptyDB_UserDoesNotExist()
        {
            using (var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB()))
                await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new GetDecksWithLearnCounts(dbContext).RunAsync(new GetDecksWithLearnCounts.Request(Guid.NewGuid())));
        }
    }
}
