using Microsoft.VisualStudio.TestTools.UnitTesting;
using MemCheck.Database;
using System.Threading.Tasks;
using MemCheck.Application.Tests.Helpers;
using System;
using MemCheck.Application.QueryValidation;

namespace MemCheck.Application.CardChanging
{
    [TestClass()]
    public class UpdateCardTests
    {
        [TestMethod()]
        public async Task UserNotLoggedIn()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, user, language: languageId);

            using var dbContext = new MemCheckDbContext(db);
            var request = UpdateCardHelper.RequestForFrontSideChanges(card, StringHelper.RandomString(), Guid.Empty);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateCard(dbContext).RunAsync(request, new TestLocalizer()));
        }
        [TestMethod()]
        public async Task UserDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, user, language: languageId);

            using var dbContext = new MemCheckDbContext(db);
            var r = UpdateCardHelper.RequestForFrontSideChanges(card, StringHelper.RandomString(), Guid.NewGuid());
            await Assert.ThrowsExceptionAsync<ApplicationException>(async () => await new UpdateCard(dbContext).RunAsync(r, new TestLocalizer()));
        }
    }
}
