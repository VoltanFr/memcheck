using MemCheck.Application.Tests.Helpers;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Ratings
{
    [TestClass()]
    public class CardRatingsTests
    {
        [TestMethod()]
        public async Task OneCardWithoutRating()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var card = await CardHelper.CreateAsync(db, user);

            using var dbContext = new MemCheckDbContext(db);
            var result = await CardRatings.LoadAsync(dbContext, Guid.Empty, card.Id);
            Assert.AreEqual(0, result.Average(card.Id));
            Assert.AreEqual(0, result.User(card.Id));
            Assert.AreEqual(0, result.Count(card.Id));
            Assert.AreEqual(card.Id, result.CardsWithoutEval.Single());
            Assert.IsFalse(result.CardsWithAverageRatingAtLeast(1).Any());
        }
        [TestMethod()]
        public async Task OneCardWithRatingByUser()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var card = await CardHelper.CreateAsync(db, user);
            var rating = RandomHelper.Rating();

            using (var dbContext = new MemCheckDbContext(db))
                await new SetCardRating(dbContext).RunAsync(new SetCardRating.Request(user, card.Id, rating));

            using (var dbContext = new MemCheckDbContext(db))
            {
                var result = await CardRatings.LoadAsync(dbContext, user, card.Id);
                Assert.AreEqual(rating, result.Average(card.Id));
                Assert.AreEqual(rating, result.User(card.Id));
                Assert.AreEqual(1, result.Count(card.Id));
                Assert.IsFalse(result.CardsWithoutEval.Any());
                Assert.IsTrue(!result.CardsWithAverageRatingAtLeast(rating + 1).Any());
                Assert.AreEqual(card.Id, result.CardsWithAverageRatingAtLeast(rating).Single());
                Assert.AreEqual(card.Id, result.CardsWithAverageRatingAtLeast(rating - 1).Single());
            }
        }
        [TestMethod()]
        public async Task OneCardWithRatingByOtherUser()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var card = await CardHelper.CreateAsync(db, user);
            var rating = RandomHelper.Rating();

            using (var dbContext = new MemCheckDbContext(db))
                await new SetCardRating(dbContext).RunAsync(new SetCardRating.Request(user, card.Id, rating));

            var otherUser = await UserHelper.CreateInDbAsync(db);

            using (var dbContext = new MemCheckDbContext(db))
            {
                var result = await CardRatings.LoadAsync(dbContext, otherUser, card.Id);
                Assert.AreEqual(rating, result.Average(card.Id));
                Assert.AreEqual(0, result.User(card.Id));
                Assert.AreEqual(1, result.Count(card.Id));
                Assert.IsFalse(result.CardsWithoutEval.Any());
                Assert.IsFalse(result.CardsWithAverageRatingAtLeast(rating + 1).Any());
                Assert.AreEqual(card.Id, result.CardsWithAverageRatingAtLeast(rating).Single());
                Assert.AreEqual(card.Id, result.CardsWithAverageRatingAtLeast(rating - 1).Single());
            }
        }
        [TestMethod()]
        public async Task Complex()
        {
            var db = DbHelper.GetEmptyTestDB();

            var user1 = await UserHelper.CreateInDbAsync(db);
            var user2 = await UserHelper.CreateInDbAsync(db);
            var card1 = await CardHelper.CreateIdAsync(db, user1);
            var card2 = await CardHelper.CreateIdAsync(db, user1);
            var card3 = await CardHelper.CreateIdAsync(db, user1);
            var card4 = await CardHelper.CreateIdAsync(db, user1);

            using (var dbContext = new MemCheckDbContext(db))
            {
                await new SetCardRating(dbContext).RunAsync(new SetCardRating.Request(user1, card1, 1));
                await new SetCardRating(dbContext).RunAsync(new SetCardRating.Request(user1, card2, 2));
                await new SetCardRating(dbContext).RunAsync(new SetCardRating.Request(user2, card2, 4));
                await new SetCardRating(dbContext).RunAsync(new SetCardRating.Request(user2, card4, 3));
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var result = await CardRatings.LoadAsync(dbContext, user1, card1, card2, card3, card4);

                Assert.AreEqual(1, result.Average(card1));
                Assert.AreEqual(3, result.Average(card2));
                Assert.AreEqual(3, result.Average(card4));
                Assert.AreEqual(0, result.Average(card3));

                Assert.AreEqual(1, result.User(card1));
                Assert.AreEqual(2, result.User(card2));
                Assert.AreEqual(0, result.User(card3));
                Assert.AreEqual(0, result.User(card4));

                Assert.AreEqual(1, result.Count(card1));
                Assert.AreEqual(2, result.Count(card2));
                Assert.AreEqual(0, result.Count(card3));
                Assert.AreEqual(1, result.Count(card4));

                Assert.AreEqual(card3, result.CardsWithoutEval.Single());

                Assert.IsFalse(result.CardsWithAverageRatingAtLeast(5).Any());
                Assert.IsFalse(result.CardsWithAverageRatingAtLeast(4).Any());
                Assert.IsTrue(result.CardsWithAverageRatingAtLeast(3).ToHashSet().SetEquals(new[] { card2, card4 }));
                Assert.IsTrue(result.CardsWithAverageRatingAtLeast(2).ToHashSet().SetEquals(new[] { card2, card4 }));
                Assert.IsTrue(result.CardsWithAverageRatingAtLeast(1).ToHashSet().SetEquals(new[] { card1, card2, card4 }));
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var result = await CardRatings.LoadAsync(dbContext, user2, card1, card2, card3, card4);

                Assert.AreEqual(1, result.Average(card1));
                Assert.AreEqual(3, result.Average(card2));
                Assert.AreEqual(3, result.Average(card4));
                Assert.AreEqual(0, result.Average(card3));

                Assert.AreEqual(0, result.User(card1));
                Assert.AreEqual(4, result.User(card2));
                Assert.AreEqual(0, result.User(card3));
                Assert.AreEqual(3, result.User(card4));

                Assert.AreEqual(1, result.Count(card1));
                Assert.AreEqual(2, result.Count(card2));
                Assert.AreEqual(0, result.Count(card3));
                Assert.AreEqual(1, result.Count(card4));

                Assert.AreEqual(card3, result.CardsWithoutEval.Single());

                Assert.IsFalse(result.CardsWithAverageRatingAtLeast(5).Any());
                Assert.IsFalse(result.CardsWithAverageRatingAtLeast(4).Any());
                Assert.IsTrue(result.CardsWithAverageRatingAtLeast(3).ToHashSet().SetEquals(new[] { card2, card4 }));
                Assert.IsTrue(result.CardsWithAverageRatingAtLeast(2).ToHashSet().SetEquals(new[] { card2, card4 }));
                Assert.IsTrue(result.CardsWithAverageRatingAtLeast(1).ToHashSet().SetEquals(new[] { card1, card2, card4 }));
            }
        }
    }
}
