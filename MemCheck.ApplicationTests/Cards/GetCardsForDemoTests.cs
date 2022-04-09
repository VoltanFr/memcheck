using MemCheck.Application.QueryValidation;
using MemCheck.Application.Tests.Helpers;
using MemCheck.Basics;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards
{
    [TestClass()]
    public class GetCardsForDemoTests
    {
        [TestMethod()]
        public async Task InvalidTag()
        {
            var db = DbHelper.GetEmptyTestDB();

            using var dbContext = new MemCheckDbContext(db);
            var request = new GetCardsForDemo.Request(Guid.Empty, Array.Empty<Guid>(), 10);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new GetCardsForDemo(dbContext.AsCallContext()).RunAsync(request));
        }
        [TestMethod()]
        public async Task NonexistentTag()
        {
            var db = DbHelper.GetEmptyTestDB();

            using var dbContext = new MemCheckDbContext(db);
            var request = new GetCardsForDemo.Request(RandomHelper.Guid(), Array.Empty<Guid>(), 10);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new GetCardsForDemo(dbContext.AsCallContext()).RunAsync(request));
        }
        [TestMethod()]
        public async Task NoCardInDb()
        {
            var db = DbHelper.GetEmptyTestDB();
            var tagId = await TagHelper.CreateAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            var request = new GetCardsForDemo.Request(tagId, Array.Empty<Guid>(), 10);
            var result = await new GetCardsForDemo(dbContext.AsCallContext()).RunAsync(request);
            Assert.IsFalse(result.Cards.Any());
        }
        [TestMethod()]
        public async Task OneCardWithoutTag()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            await CardHelper.CreateAsync(db, user);

            var tagId = await TagHelper.CreateAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            var request = new GetCardsForDemo.Request(tagId, Array.Empty<Guid>(), 10);
            var result = await new GetCardsForDemo(dbContext.AsCallContext()).RunAsync(request);
            Assert.IsFalse(result.Cards.Any());
        }
        [TestMethod()]
        public async Task OneCardWithOtherTag()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var otherTagId = await TagHelper.CreateAsync(db);
            await CardHelper.CreateAsync(db, user, tagIds: otherTagId.AsArray());

            var tagId = await TagHelper.CreateAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            var request = new GetCardsForDemo.Request(tagId, Array.Empty<Guid>(), 10);
            var result = await new GetCardsForDemo(dbContext.AsCallContext()).RunAsync(request);
            Assert.IsFalse(result.Cards.Any());
        }
        [TestMethod()]
        public async Task OneCardWithTheRequestedTag()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var tagId = await TagHelper.CreateAsync(db);
            var cardId = await CardHelper.CreateIdAsync(db, user, tagIds: tagId.AsArray());

            using var dbContext = new MemCheckDbContext(db);
            var request = new GetCardsForDemo.Request(tagId, Array.Empty<Guid>(), 10);
            var result = await new GetCardsForDemo(dbContext.AsCallContext()).RunAsync(request);
            Assert.AreEqual(1, result.Cards.Count());
            Assert.AreEqual(cardId, result.Cards.Single().CardId);
        }
        [TestMethod()]
        public async Task OneExcludedCard()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var tagId = await TagHelper.CreateAsync(db);
            var cardId = await CardHelper.CreateIdAsync(db, user, tagIds: tagId.AsArray());

            using var dbContext = new MemCheckDbContext(db);
            var request = new GetCardsForDemo.Request(tagId, cardId.AsArray(), 10);
            var result = await new GetCardsForDemo(dbContext.AsCallContext()).RunAsync(request);
            Assert.IsFalse(result.Cards.Any());
        }
        [TestMethod()]
        public async Task ExcludedCardInList()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var tagId = await TagHelper.CreateAsync(db);
            var excludedCardId = await CardHelper.CreateIdAsync(db, user, tagIds: tagId.AsArray());
            for (int i = 0; i < 9; i++)
                await CardHelper.CreateIdAsync(db, user, tagIds: tagId.AsArray());

            using var dbContext = new MemCheckDbContext(db);
            var request = new GetCardsForDemo.Request(tagId, excludedCardId.AsArray(), 10);
            var result = await new GetCardsForDemo(dbContext.AsCallContext()).RunAsync(request);
            Assert.AreEqual(9, result.Cards.Count());
            Assert.IsFalse(result.Cards.Any(card => card.CardId == excludedCardId));
        }
        [TestMethod()]
        public async Task OneCardWithTheRequestedTagAndAnotherOne()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var tagId = await TagHelper.CreateAsync(db);
            var otherTagId = await TagHelper.CreateAsync(db);
            var cardId = await CardHelper.CreateIdAsync(db, user, tagIds: new[] { tagId, otherTagId });

            using var dbContext = new MemCheckDbContext(db);
            var request = new GetCardsForDemo.Request(tagId, Array.Empty<Guid>(), 10);
            var result = await new GetCardsForDemo(dbContext.AsCallContext()).RunAsync(request);
            Assert.AreEqual(1, result.Cards.Count());
            Assert.AreEqual(cardId, result.Cards.Single().CardId);
        }
        [TestMethod()]
        public async Task ObtainCorrectCount()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var tagId = await TagHelper.CreateAsync(db);
            var createdCount = RandomNumberGenerator.GetInt32(5, 20);
            for (int i = 0; i < createdCount; i++)
                await CardHelper.CreateIdAsync(db, user, tagIds: tagId.AsArray());

            using var dbContext = new MemCheckDbContext(db);
            var countRequested = RandomNumberGenerator.GetInt32(1, createdCount - 2);
            var request = new GetCardsForDemo.Request(tagId, Array.Empty<Guid>(), countRequested);
            var result = await new GetCardsForDemo(dbContext.AsCallContext()).RunAsync(request);
            Assert.AreEqual(countRequested, result.Cards.Count());
        }
        [TestMethod()]
        public async Task TestResultFields()
        {
            var db = DbHelper.GetEmptyTestDB();
            var userName = RandomHelper.String();
            var cardAuthor = await UserHelper.CreateInDbAsync(db, userName: userName);
            await UserHelper.CreateInDbAsync(db);   //So that another user exists
            var tagName = RandomHelper.String();
            var tagId = await TagHelper.CreateAsync(db, tagName);
            var otherTagName = RandomHelper.String();
            var otherTagId = await TagHelper.CreateAsync(db, otherTagName);
            var frontSideImageName = RandomHelper.String();
            var frontSideImageId = await ImageHelper.CreateAsync(db, cardAuthor, frontSideImageName);
            var additionalInfoImageName = RandomHelper.String();
            var additionalInfoImageId = await ImageHelper.CreateAsync(db, cardAuthor, additionalInfoImageName);
            var card = await CardHelper.CreateAsync(db, cardAuthor, tagIds: new[] { otherTagId, tagId }, frontSideImages: frontSideImageId.AsArray(), additionalSideImages: additionalInfoImageId.AsArray());
            await RatingHelper.RecordForUserAsync(db, cardAuthor, card.Id, 3);
            var otherUser = await UserHelper.CreateInDbAsync(db);
            await RatingHelper.RecordForUserAsync(db, otherUser, card.Id, 5);

            using var dbContext = new MemCheckDbContext(db);
            var request = new GetCardsForDemo.Request(tagId, Array.Empty<Guid>(), 10);
            var result = await new GetCardsForDemo(dbContext.AsCallContext()).RunAsync(request);
            Assert.AreEqual(1, result.Cards.Count());
            var resultCard = result.Cards.Single();
            Assert.AreEqual(card.Id, resultCard.CardId);
            Assert.AreEqual(card.VersionUtcDate, resultCard.LastChangeUtcTime);
            Assert.AreEqual(card.FrontSide, resultCard.FrontSide);
            Assert.AreEqual(card.BackSide, resultCard.BackSide);
            Assert.AreEqual(card.AdditionalInfo, resultCard.AdditionalInfo);
            Assert.AreEqual(card.References, resultCard.References);
            Assert.AreEqual(userName, resultCard.VersionCreator);
            Assert.AreEqual(4, resultCard.AverageRating);
            Assert.AreEqual(2, resultCard.CountOfUserRatings);
            Assert.IsFalse(resultCard.IsInFrench);
            CollectionAssert.AreEquivalent(new[] { tagName, otherTagName }, resultCard.Tags.ToArray());
            Assert.AreEqual(2, resultCard.Images.Count());
            var resultFrontSideImage = resultCard.Images.Single(img => img.CardSide == ImageInCard.FrontSide);
            Assert.AreEqual(frontSideImageName, resultFrontSideImage.Name);
            Assert.AreEqual(frontSideImageId, resultFrontSideImage.ImageId);
            var resultAdditionalInfoImage = resultCard.Images.Single(img => img.CardSide == ImageInCard.AdditionalInfo);
            Assert.AreEqual(additionalInfoImageName, resultAdditionalInfoImage.Name);
            Assert.AreEqual(additionalInfoImageId, resultAdditionalInfoImage.ImageId);
        }
        [TestMethod()]
        public async Task RightRating()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var tagId = await TagHelper.CreateAsync(db);

            var cardsWithRating4 = new List<Guid>();
            var cardsWithRating3 = new List<Guid>();

            for (int rating = 0; rating <= 4; rating++)
                for (int i = 0; i < 3; i++)
                {
                    var card = await CardHelper.CreateAsync(db, user, tagIds: tagId.AsArray());
                    if (rating != 0)
                        await RatingHelper.RecordForUserAsync(db, user, card.Id, rating);
                    if (rating == 4)
                        cardsWithRating4.Add(card.Id);
                    if (rating == 3)
                        cardsWithRating3.Add(card.Id);
                }

            using var dbContext = new MemCheckDbContext(db);
            var request = new GetCardsForDemo.Request(tagId, Array.Empty<Guid>(), 4);
            var result = (await new GetCardsForDemo(dbContext.AsCallContext()).RunAsync(request)).Cards.Select(c => c.CardId).ToImmutableHashSet();

            CollectionAssert.IsSubsetOf(cardsWithRating4, result);
            var cardNotWithRating4 = result.Single(card => !cardsWithRating4.Contains(card));
            Assert.IsTrue(cardsWithRating3.Contains(cardNotWithRating4));
        }
        [TestMethod()]
        public async Task RightRatingAndNotTheSameCardsOnSuccessiveRuns()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var tagId = await TagHelper.CreateAsync(db);
            var cardsWithRating5 = new List<Guid>();

            //100 cards with each rating value
            for (int rating = 0; rating < 6; rating++)
                for (int i = 0; i < 100; i++)
                {
                    var card = await CardHelper.CreateAsync(db, user, tagIds: tagId.AsArray());
                    if (rating != 0)
                        await RatingHelper.RecordForUserAsync(db, user, card.Id, rating);
                    if (rating == 5)
                        cardsWithRating5.Add(card.Id);
                }

            using var dbContext = new MemCheckDbContext(db);
            const int requestCardCount = 10;

            var request = new GetCardsForDemo.Request(tagId, Array.Empty<Guid>(), requestCardCount);

            var firstRunCards = (await new GetCardsForDemo(dbContext.AsCallContext()).RunAsync(request)).Cards.Select(c => c.CardId).ToImmutableHashSet();
            Assert.AreEqual(requestCardCount, firstRunCards.Count);
            CollectionAssert.IsSubsetOf(firstRunCards, cardsWithRating5);

            var secondRunCards = (await new GetCardsForDemo(dbContext.AsCallContext()).RunAsync(request)).Cards.Select(c => c.CardId).ToImmutableHashSet();
            Assert.AreEqual(requestCardCount, secondRunCards.Count);
            CollectionAssert.IsSubsetOf(secondRunCards, cardsWithRating5);

            var thirdRunCards = (await new GetCardsForDemo(dbContext.AsCallContext()).RunAsync(request)).Cards.Select(c => c.CardId).ToImmutableHashSet();
            Assert.AreEqual(requestCardCount, thirdRunCards.Count);
            CollectionAssert.IsSubsetOf(thirdRunCards, cardsWithRating5);

            Assert.IsFalse(firstRunCards.SetEquals(secondRunCards));
            Assert.IsFalse(firstRunCards.SetEquals(thirdRunCards));
            Assert.IsFalse(secondRunCards.SetEquals(thirdRunCards));
        }
        [TestMethod()]
        public async Task OneCardPrivate()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var tagId = await TagHelper.CreateAsync(db);
            await CardHelper.CreateAsync(db, user, tagIds: tagId.AsArray(), userWithViewIds: user.AsArray());

            using var dbContext = new MemCheckDbContext(db);
            var request = new GetCardsForDemo.Request(tagId, Array.Empty<Guid>(), 10);
            var result = await new GetCardsForDemo(dbContext.AsCallContext()).RunAsync(request);
            Assert.IsFalse(result.Cards.Any());
        }
        [TestMethod()]
        public async Task OneCardVisibleToSomeUsers()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user1 = await UserHelper.CreateInDbAsync(db);
            var user2 = await UserHelper.CreateInDbAsync(db);
            var tagId = await TagHelper.CreateAsync(db);
            await CardHelper.CreateAsync(db, user1, tagIds: tagId.AsArray(), userWithViewIds: new[] { user1, user2 });

            using var dbContext = new MemCheckDbContext(db);
            var request = new GetCardsForDemo.Request(tagId, Array.Empty<Guid>(), 10);
            var result = await new GetCardsForDemo(dbContext.AsCallContext()).RunAsync(request);
            Assert.IsFalse(result.Cards.Any());
        }
        [TestMethod()]
        public async Task TwoCardsWithOnlyOnePublic()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var tagId = await TagHelper.CreateAsync(db);
            await CardHelper.CreateAsync(db, user, tagIds: tagId.AsArray(), userWithViewIds: user.AsArray());
            var publicCard = await CardHelper.CreateAsync(db, user, tagIds: tagId.AsArray());

            using var dbContext = new MemCheckDbContext(db);
            var request = new GetCardsForDemo.Request(tagId, Array.Empty<Guid>(), 10);
            var result = await new GetCardsForDemo(dbContext.AsCallContext()).RunAsync(request);
            Assert.AreEqual(1, result.Cards.Count());
            Assert.AreEqual(publicCard.Id, result.Cards.Single().CardId);
        }
    }
}
