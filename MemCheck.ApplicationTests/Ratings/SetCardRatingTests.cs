using MemCheck.Application.QueryValidation;
using MemCheck.Application.Tests.Helpers;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace MemCheck.Application.Ratings
{
    [TestClass()]
    public class SetCardRatingTests
    {
        [TestMethod()]
        public async Task UserNotLoggedIn()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateIdAsync(db, user, language: languageId);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new SetCardRating(dbContext).RunAsync(new SetCardRating.Request(Guid.Empty, card, RandomHelper.Rating())));
        }
        [TestMethod()]
        public async Task UserDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateIdAsync(db, user, language: languageId);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<DbUpdateException>(async () => await new SetCardRating(dbContext).RunAsync(new SetCardRating.Request(Guid.NewGuid(), card, RandomHelper.Rating())));
        }
        [TestMethod()]
        public async Task TooSmall()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateIdAsync(db, user, language: languageId);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new SetCardRating(dbContext).RunAsync(new SetCardRating.Request(user, card, 0)));
        }
        [TestMethod()]
        public async Task TooBig()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateIdAsync(db, user, language: languageId);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new SetCardRating(dbContext).RunAsync(new SetCardRating.Request(user, card, 6)));
        }
        [TestMethod()]
        public async Task NoPreviousValue()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateIdAsync(db, user, language: languageId);
            var rating = RandomHelper.Rating();

            using (var dbContext = new MemCheckDbContext(db))
                await new SetCardRating(dbContext).RunAsync(new SetCardRating.Request(user, card, rating));

            using (var dbContext = new MemCheckDbContext(db))
            {
                var ratings = await CardRatings.LoadAsync(dbContext, user, card);
                Assert.AreEqual(rating, ratings.User(card));
            }
        }
        [TestMethod()]
        public async Task PreviousValue()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateIdAsync(db, user, language: languageId);

            using (var dbContext = new MemCheckDbContext(db))
                await new SetCardRating(dbContext).RunAsync(new SetCardRating.Request(user, card, 1));
            using (var dbContext = new MemCheckDbContext(db))
                await new SetCardRating(dbContext).RunAsync(new SetCardRating.Request(user, card, 5));

            using (var dbContext = new MemCheckDbContext(db))
            {
                var ratings = await CardRatings.LoadAsync(dbContext, user, card);
                Assert.AreEqual(5, ratings.User(card));
            }
        }
        [TestMethod()]
        public async Task Complex()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user1 = await UserHelper.CreateInDbAsync(db);
            var user2 = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateIdAsync(db, user1, language: languageId);

            using (var dbContext = new MemCheckDbContext(db))
                await new SetCardRating(dbContext).RunAsync(new SetCardRating.Request(user1, card, 1));
            using (var dbContext = new MemCheckDbContext(db))
                await new SetCardRating(dbContext).RunAsync(new SetCardRating.Request(user2, card, 5));

            using (var dbContext = new MemCheckDbContext(db))
            {
                var ratings = await CardRatings.LoadAsync(dbContext, user1, card);
                Assert.AreEqual(1, ratings.User(card));
                Assert.AreEqual(3, ratings.Average(card));
            }

            using (var dbContext = new MemCheckDbContext(db))
                await new SetCardRating(dbContext).RunAsync(new SetCardRating.Request(user1, card, 5));

            using (var dbContext = new MemCheckDbContext(db))
            {
                var ratings = await CardRatings.LoadAsync(dbContext, user1, card);
                Assert.AreEqual(5, ratings.User(card));
                Assert.AreEqual(5, ratings.Average(card));
            }
        }
    }
}
