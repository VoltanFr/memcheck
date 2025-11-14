using MemCheck.Application.Heaping;
using MemCheck.Application.Helpers;
using MemCheck.Application.QueryValidation;
using MemCheck.Basics;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards;

[TestClass()]
public class GetCardsForDemoTests
{
    [TestMethod()]
    public async Task InvalidTag()
    {
        var db = DbHelper.GetEmptyTestDB();

        using var dbContext = new MemCheckDbContext(db);
        var request = new GetCardsForDemo.Request(Guid.Empty, Array.Empty<Guid>(), 10);
        await Assert.ThrowsExactlyAsync<RequestInputException>(async () => await new GetCardsForDemo(dbContext.AsCallContext()).RunAsync(request));
        Assert.IsFalse(dbContext.DemoDownloadAuditTrailEntries.Any());
    }
    [TestMethod()]
    public async Task NonexistentTag()
    {
        var db = DbHelper.GetEmptyTestDB();

        using var dbContext = new MemCheckDbContext(db);
        var request = new GetCardsForDemo.Request(RandomHelper.Guid(), Array.Empty<Guid>(), 10);
        await Assert.ThrowsExactlyAsync<RequestInputException>(async () => await new GetCardsForDemo(dbContext.AsCallContext()).RunAsync(request));
        Assert.IsFalse(dbContext.DemoDownloadAuditTrailEntries.Any());
    }
    [TestMethod()]
    public async Task NoCardInDb()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var tagId = await TagHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        var request = new GetCardsForDemo.Request(tagId, Array.Empty<Guid>(), 10);
        var runDate = RandomHelper.Date();
        var result = await new GetCardsForDemo(dbContext.AsCallContext(), runDate).RunAsync(request);
        Assert.IsFalse(result.Cards.Any());
        Assert.AreEqual(1, dbContext.DemoDownloadAuditTrailEntries.Count());
        var demoDownloadAuditTrailEntry = dbContext.DemoDownloadAuditTrailEntries.Single();
        Assert.AreEqual(tagId, demoDownloadAuditTrailEntry.TagId);
        Assert.AreEqual(runDate, demoDownloadAuditTrailEntry.DownloadUtcDate);
        Assert.AreEqual(0, demoDownloadAuditTrailEntry.CountOfCardsReturned);
        Assert.AreNotEqual(Guid.Empty, demoDownloadAuditTrailEntry.Id);
    }
    [TestMethod()]
    public async Task OneCardWithoutTag()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        await CardHelper.CreateAsync(db, user.Id);

        var tagId = await TagHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        var request = new GetCardsForDemo.Request(tagId, Array.Empty<Guid>(), 10);
        var runDate = RandomHelper.Date();
        var result = await new GetCardsForDemo(dbContext.AsCallContext(), runDate).RunAsync(request);
        Assert.IsFalse(result.Cards.Any());
        Assert.AreEqual(1, dbContext.DemoDownloadAuditTrailEntries.Count());
        var demoDownloadAuditTrailEntry = dbContext.DemoDownloadAuditTrailEntries.Single();
        Assert.AreEqual(tagId, demoDownloadAuditTrailEntry.TagId);
        Assert.AreEqual(runDate, demoDownloadAuditTrailEntry.DownloadUtcDate);
        Assert.AreEqual(0, demoDownloadAuditTrailEntry.CountOfCardsReturned);
        Assert.AreNotEqual(Guid.Empty, demoDownloadAuditTrailEntry.Id);
    }
    [TestMethod()]
    public async Task OneCardWithOtherTag()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var otherTagId = await TagHelper.CreateAsync(db, user);
        await CardHelper.CreateAsync(db, user.Id, tagIds: otherTagId.AsArray());

        var tagId = await TagHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        var request = new GetCardsForDemo.Request(tagId, Array.Empty<Guid>(), 10);
        var runDate = RandomHelper.Date();
        var result = await new GetCardsForDemo(dbContext.AsCallContext(), runDate).RunAsync(request);
        Assert.IsFalse(result.Cards.Any());
        Assert.AreEqual(1, dbContext.DemoDownloadAuditTrailEntries.Count());
        var demoDownloadAuditTrailEntry = dbContext.DemoDownloadAuditTrailEntries.Single();
        Assert.AreEqual(tagId, demoDownloadAuditTrailEntry.TagId);
        Assert.AreEqual(runDate, demoDownloadAuditTrailEntry.DownloadUtcDate);
        Assert.AreEqual(0, demoDownloadAuditTrailEntry.CountOfCardsReturned);
        Assert.AreNotEqual(Guid.Empty, demoDownloadAuditTrailEntry.Id);
    }
    [TestMethod()]
    public async Task OneCardWithTheRequestedTag()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var tagId = await TagHelper.CreateAsync(db, user);
        var cardId = await CardHelper.CreateIdAsync(db, user.Id, tagIds: tagId.AsArray());

        using var dbContext = new MemCheckDbContext(db);
        var request = new GetCardsForDemo.Request(tagId, Array.Empty<Guid>(), 10);
        var runDate = RandomHelper.Date();
        var result = await new GetCardsForDemo(dbContext.AsCallContext(), runDate).RunAsync(request);
        Assert.AreEqual(1, result.Cards.Count());
        Assert.AreEqual(cardId, result.Cards.Single().CardId);
        Assert.AreEqual(1, dbContext.DemoDownloadAuditTrailEntries.Count());
        var demoDownloadAuditTrailEntry = dbContext.DemoDownloadAuditTrailEntries.Single();
        Assert.AreEqual(tagId, demoDownloadAuditTrailEntry.TagId);
        Assert.AreEqual(runDate, demoDownloadAuditTrailEntry.DownloadUtcDate);
        Assert.AreEqual(1, demoDownloadAuditTrailEntry.CountOfCardsReturned);
        Assert.AreNotEqual(Guid.Empty, demoDownloadAuditTrailEntry.Id);
    }
    [TestMethod()]
    public async Task OneExcludedCard()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var tagId = await TagHelper.CreateAsync(db, user);
        var cardId = await CardHelper.CreateIdAsync(db, user.Id, tagIds: tagId.AsArray());

        using var dbContext = new MemCheckDbContext(db);
        var request = new GetCardsForDemo.Request(tagId, cardId.AsArray(), 10);
        var runDate = RandomHelper.Date();
        var result = await new GetCardsForDemo(dbContext.AsCallContext(), runDate).RunAsync(request);
        Assert.IsFalse(result.Cards.Any());
        Assert.AreEqual(1, dbContext.DemoDownloadAuditTrailEntries.Count());
        var demoDownloadAuditTrailEntry = dbContext.DemoDownloadAuditTrailEntries.Single();
        Assert.AreEqual(tagId, demoDownloadAuditTrailEntry.TagId);
        Assert.AreEqual(runDate, demoDownloadAuditTrailEntry.DownloadUtcDate);
        Assert.AreEqual(0, demoDownloadAuditTrailEntry.CountOfCardsReturned);
        Assert.AreNotEqual(Guid.Empty, demoDownloadAuditTrailEntry.Id);
    }
    [TestMethod()]
    public async Task ExcludedCardInList()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var tagId = await TagHelper.CreateAsync(db, user);
        var excludedCardId = await CardHelper.CreateIdAsync(db, user.Id, tagIds: tagId.AsArray());
        for (var i = 0; i < 9; i++)
            await CardHelper.CreateIdAsync(db, user.Id, tagIds: tagId.AsArray());

        using var dbContext = new MemCheckDbContext(db);
        var request = new GetCardsForDemo.Request(tagId, excludedCardId.AsArray(), 10);
        var runDate = RandomHelper.Date();
        var result = await new GetCardsForDemo(dbContext.AsCallContext(), runDate).RunAsync(request);
        Assert.AreEqual(9, result.Cards.Count());
        Assert.IsFalse(result.Cards.Any(card => card.CardId == excludedCardId));
        Assert.AreEqual(1, dbContext.DemoDownloadAuditTrailEntries.Count());
        var demoDownloadAuditTrailEntry = dbContext.DemoDownloadAuditTrailEntries.Single();
        Assert.AreEqual(tagId, demoDownloadAuditTrailEntry.TagId);
        Assert.AreEqual(runDate, demoDownloadAuditTrailEntry.DownloadUtcDate);
        Assert.AreEqual(9, demoDownloadAuditTrailEntry.CountOfCardsReturned);
        Assert.AreNotEqual(Guid.Empty, demoDownloadAuditTrailEntry.Id);
    }
    [TestMethod()]
    public async Task OneCardWithTheRequestedTagAndAnotherOne()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var tagId = await TagHelper.CreateAsync(db, user);
        var otherTagId = await TagHelper.CreateAsync(db, user);
        var cardId = await CardHelper.CreateIdAsync(db, user.Id, tagIds: new[] { tagId, otherTagId });

        using var dbContext = new MemCheckDbContext(db);
        var request = new GetCardsForDemo.Request(tagId, Array.Empty<Guid>(), 10);
        var runDate = RandomHelper.Date();
        var result = await new GetCardsForDemo(dbContext.AsCallContext(), runDate).RunAsync(request);
        Assert.AreEqual(1, result.Cards.Count());
        Assert.AreEqual(cardId, result.Cards.Single().CardId);
        Assert.AreEqual(1, dbContext.DemoDownloadAuditTrailEntries.Count());
        var demoDownloadAuditTrailEntry = dbContext.DemoDownloadAuditTrailEntries.Single();
        Assert.AreEqual(tagId, demoDownloadAuditTrailEntry.TagId);
        Assert.AreEqual(runDate, demoDownloadAuditTrailEntry.DownloadUtcDate);
        Assert.AreEqual(1, demoDownloadAuditTrailEntry.CountOfCardsReturned);
        Assert.AreNotEqual(Guid.Empty, demoDownloadAuditTrailEntry.Id);
    }
    [TestMethod()]
    public async Task ObtainCorrectCount()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var tagId = await TagHelper.CreateAsync(db, user);
        var createdCount = RandomHelper.Int(5, 20);
        for (var i = 0; i < createdCount; i++)
            await CardHelper.CreateIdAsync(db, user.Id, tagIds: tagId.AsArray());

        using var dbContext = new MemCheckDbContext(db);
        var countRequested = RandomHelper.Int(1, createdCount - 2);
        var request = new GetCardsForDemo.Request(tagId, Array.Empty<Guid>(), countRequested);
        var runDate = RandomHelper.Date();
        var result = await new GetCardsForDemo(dbContext.AsCallContext(), runDate).RunAsync(request);
        Assert.AreEqual(countRequested, result.Cards.Count());
        Assert.AreEqual(1, dbContext.DemoDownloadAuditTrailEntries.Count());
        var demoDownloadAuditTrailEntry = dbContext.DemoDownloadAuditTrailEntries.Single();
        Assert.AreEqual(tagId, demoDownloadAuditTrailEntry.TagId);
        Assert.AreEqual(runDate, demoDownloadAuditTrailEntry.DownloadUtcDate);
        Assert.AreEqual(countRequested, demoDownloadAuditTrailEntry.CountOfCardsReturned);
        Assert.AreNotEqual(Guid.Empty, demoDownloadAuditTrailEntry.Id);
    }
    [TestMethod()]
    public async Task TestResultFields()
    {
        var db = DbHelper.GetEmptyTestDB();
        var cardAuthor = await UserHelper.CreateUserInDbAsync(db);
        await UserHelper.CreateInDbAsync(db);   //So that another user exists
        var tagName = RandomHelper.String();
        var tagId = await TagHelper.CreateAsync(db, cardAuthor, tagName);
        var otherTagName = RandomHelper.String();
        var otherTagId = await TagHelper.CreateAsync(db, cardAuthor, otherTagName);
        var card = await CardHelper.CreateAsync(db, cardAuthor.Id, tagIds: new[] { otherTagId, tagId });
        await RatingHelper.RecordForUserAsync(db, cardAuthor.Id, card.Id, 3);
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
        Assert.AreEqual(cardAuthor.UserName, resultCard.VersionCreator);
        Assert.AreEqual(4, resultCard.AverageRating);
        Assert.AreEqual(2, resultCard.CountOfUserRatings);
        Assert.IsFalse(resultCard.IsInFrench);
        CollectionAssert.AreEquivalent(new[] { tagName, otherTagName }, resultCard.Tags.ToArray());
    }
    [TestMethod()]
    public async Task RightRating()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var tagId = await TagHelper.CreateAsync(db, user);

        var cardsWithRating4 = new List<Guid>();
        var cardsWithRating3 = new List<Guid>();

        for (var rating = 0; rating <= 4; rating++)
            for (var i = 0; i < 3; i++)
            {
                var card = await CardHelper.CreateAsync(db, user.Id, tagIds: tagId.AsArray());
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
        var user = await UserHelper.CreateUserInDbAsync(db);
        var tagId = await TagHelper.CreateAsync(db, user);
        var cardsWithRating5 = new List<Guid>();

        //100 cards with each rating value
        for (var rating = 0; rating < 6; rating++)
            for (var i = 0; i < 100; i++)
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

        var firstRunDate = RandomHelper.Date();
        var firstRunCards = (await new GetCardsForDemo(dbContext.AsCallContext(), firstRunDate).RunAsync(request)).Cards.Select(c => c.CardId).ToImmutableHashSet();
        Assert.AreEqual(requestCardCount, firstRunCards.Count);
        CollectionAssert.IsSubsetOf(firstRunCards, cardsWithRating5);

        var secondRunDate = RandomHelper.Date();
        var secondRunCards = (await new GetCardsForDemo(dbContext.AsCallContext(), secondRunDate).RunAsync(request)).Cards.Select(c => c.CardId).ToImmutableHashSet();
        Assert.AreEqual(requestCardCount, secondRunCards.Count);
        CollectionAssert.IsSubsetOf(secondRunCards, cardsWithRating5);

        var thirdRunDate = RandomHelper.Date();
        var thirdRunCards = (await new GetCardsForDemo(dbContext.AsCallContext(), thirdRunDate).RunAsync(request)).Cards.Select(c => c.CardId).ToImmutableHashSet();
        Assert.HasCount(requestCardCount, thirdRunCards);
        CollectionAssert.IsSubsetOf(thirdRunCards, cardsWithRating5);

        Assert.IsFalse(firstRunCards.SetEquals(secondRunCards));
        Assert.IsFalse(firstRunCards.SetEquals(thirdRunCards));
        Assert.IsFalse(secondRunCards.SetEquals(thirdRunCards));

        Assert.AreEqual(3, dbContext.DemoDownloadAuditTrailEntries.Count());

        var firstRunAuditTrailEntry = dbContext.DemoDownloadAuditTrailEntries.Single(entry => entry.DownloadUtcDate == firstRunDate);
        Assert.AreEqual(tagId, firstRunAuditTrailEntry.TagId);
        Assert.AreEqual(requestCardCount, firstRunAuditTrailEntry.CountOfCardsReturned);

        var secondRunAuditTrailEntry = dbContext.DemoDownloadAuditTrailEntries.Single(entry => entry.DownloadUtcDate == secondRunDate);
        Assert.AreEqual(tagId, secondRunAuditTrailEntry.TagId);
        Assert.AreEqual(requestCardCount, secondRunAuditTrailEntry.CountOfCardsReturned);

        var thirdRunAuditTrailEntry = dbContext.DemoDownloadAuditTrailEntries.Single(entry => entry.DownloadUtcDate == thirdRunDate);
        Assert.AreEqual(tagId, thirdRunAuditTrailEntry.TagId);
        Assert.AreEqual(requestCardCount, thirdRunAuditTrailEntry.CountOfCardsReturned);
    }
    [TestMethod()]
    public async Task OneCardPrivate()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var tagId = await TagHelper.CreateAsync(db, user);
        await CardHelper.CreateAsync(db, user, tagIds: tagId.AsArray(), usersWithView: user.AsArray());

        using var dbContext = new MemCheckDbContext(db);
        var request = new GetCardsForDemo.Request(tagId, Array.Empty<Guid>(), 10);
        var runDate = RandomHelper.Date();
        var result = await new GetCardsForDemo(dbContext.AsCallContext(), runDate).RunAsync(request);
        Assert.IsFalse(result.Cards.Any());

        Assert.AreEqual(1, dbContext.DemoDownloadAuditTrailEntries.Count());
        var demoDownloadAuditTrailEntry = dbContext.DemoDownloadAuditTrailEntries.Single();
        Assert.AreEqual(tagId, demoDownloadAuditTrailEntry.TagId);
        Assert.AreEqual(runDate, demoDownloadAuditTrailEntry.DownloadUtcDate);
        Assert.AreEqual(0, demoDownloadAuditTrailEntry.CountOfCardsReturned);
    }
    [TestMethod()]
    public async Task OneCardVisibleToSomeUsers()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user1 = await UserHelper.CreateUserInDbAsync(db);
        var user2 = await UserHelper.CreateInDbAsync(db);
        var tagId = await TagHelper.CreateAsync(db, user1);
        await CardHelper.CreateAsync(db, user1.Id, tagIds: tagId.AsArray(), userWithViewIds: new[] { user1.Id, user2 });

        using var dbContext = new MemCheckDbContext(db);
        var request = new GetCardsForDemo.Request(tagId, Array.Empty<Guid>(), 10);
        var runDate = RandomHelper.Date();
        var result = await new GetCardsForDemo(dbContext.AsCallContext(), runDate).RunAsync(request);
        Assert.IsFalse(result.Cards.Any());

        Assert.AreEqual(1, dbContext.DemoDownloadAuditTrailEntries.Count());
        var demoDownloadAuditTrailEntry = dbContext.DemoDownloadAuditTrailEntries.Single();
        Assert.AreEqual(tagId, demoDownloadAuditTrailEntry.TagId);
        Assert.AreEqual(runDate, demoDownloadAuditTrailEntry.DownloadUtcDate);
        Assert.AreEqual(0, demoDownloadAuditTrailEntry.CountOfCardsReturned);
    }
    [TestMethod()]
    public async Task TwoCardsWithOnlyOnePublic()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var tagId = await TagHelper.CreateAsync(db, user);
        await CardHelper.CreateAsync(db, user, tagIds: tagId.AsArray(), usersWithView: user.AsArray());
        var publicCard = await CardHelper.CreateAsync(db, user, tagIds: tagId.AsArray());

        using var dbContext = new MemCheckDbContext(db);
        var request = new GetCardsForDemo.Request(tagId, Array.Empty<Guid>(), 10);
        var runDate = RandomHelper.Date();
        var result = await new GetCardsForDemo(dbContext.AsCallContext(), runDate).RunAsync(request);
        Assert.AreEqual(1, result.Cards.Count());
        Assert.AreEqual(publicCard.Id, result.Cards.Single().CardId);

        Assert.AreEqual(1, dbContext.DemoDownloadAuditTrailEntries.Count());
        var demoDownloadAuditTrailEntry = dbContext.DemoDownloadAuditTrailEntries.Single();
        Assert.AreEqual(tagId, demoDownloadAuditTrailEntry.TagId);
        Assert.AreEqual(runDate, demoDownloadAuditTrailEntry.DownloadUtcDate);
        Assert.AreEqual(1, demoDownloadAuditTrailEntry.CountOfCardsReturned);
        Assert.AreNotEqual(Guid.Empty, demoDownloadAuditTrailEntry.Id);
    }
    [TestMethod()]
    public async Task RightOrder()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var tagId = await TagHelper.CreateAsync(db, user);

        for (var cardIndex = 0; cardIndex <= GetCardsForDemo.Request.MaxCount; cardIndex++)
        {
            var card = await CardHelper.CreateAsync(db, user, tagIds: tagId.AsArray());
            await RatingHelper.RecordForUserAsync(db, user, card.Id, RandomHelper.Rating());
        }

        using var dbContext = new MemCheckDbContext(db);
        var request = new GetCardsForDemo.Request(tagId, Array.Empty<Guid>(), GetCardsForDemo.Request.MaxCount);
        var result = (await new GetCardsForDemo(dbContext.AsCallContext()).RunAsync(request)).Cards.ToImmutableArray();

        Assert.AreEqual(GetCardsForDemo.Request.MaxCount, result.Length);

        var expectedOrder = result.OrderByDescending(card => card.AverageRating).ToImmutableArray();
        Assert.IsTrue(expectedOrder.SequenceEqual(result));
    }
    [TestMethod()]
    public async Task TwoCardsWithCheckingOfAllFields()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user.Id, algorithmId: UnitTestsHeapingAlgorithm.ID);
        var french = await CardLanguageHelper.CreateAsync(db, "Français");
        var otherLanguage = await CardLanguageHelper.CreateAsync(db);
        var searchedTagName = RandomHelper.String();
        var searchedTag = await TagHelper.CreateAsync(db, user, searchedTagName);

        var card1VersionDate = RandomHelper.Date();
        var card1 = await CardHelper.CreateAsync(db, user.Id, versionDate: card1VersionDate, language: french, tagIds: searchedTag.AsArray());
        var card1LastLearnTime = CardInDeck.NeverLearntLastLearnTime;
        var card1BiggestHeapReached = RandomHelper.Heap();
        var card1NbTimesInNotLearnedHeap = RandomHelper.Int(CardInDeck.MaxHeapValue);
        var card1Rating = RandomHelper.Rating();
        await RatingHelper.RecordForUserAsync(db, user.Id, card1.Id, card1Rating);

        var otherTagName = RandomHelper.String();
        var otherTag = await TagHelper.CreateAsync(db, user, otherTagName);
        var card2VersionDate = RandomHelper.Date();
        var card2 = await CardHelper.CreateAsync(db, user.Id, versionDate: card2VersionDate, language: otherLanguage, tagIds: new[] { searchedTag, otherTag });
        var card2BiggestHeapReached = RandomHelper.Heap();
        var card2NbTimesInNotLearnedHeap = RandomHelper.Int(CardInDeck.MaxHeapValue);

        using var dbContext = new MemCheckDbContext(db);
        var request = new GetCardsForDemo.Request(searchedTag, Array.Empty<Guid>(), 10);
        var runDate = RandomHelper.Date();
        var result = (await new GetCardsForDemo(dbContext.AsCallContext(), runDate).RunAsync(request)).Cards;
        Assert.AreEqual(2, result.Count());

        {
            var card1FromResult = result.Single(card => card.CardId == card1.Id);
            Assert.AreEqual(card1VersionDate, card1FromResult.LastChangeUtcTime);
            Assert.AreEqual(card1.FrontSide, card1FromResult.FrontSide);
            Assert.AreEqual(card1.BackSide, card1FromResult.BackSide);
            Assert.AreEqual(card1.AdditionalInfo, card1FromResult.AdditionalInfo);
            Assert.AreEqual(card1.References, card1FromResult.References);
            Assert.AreEqual(user.UserName, card1FromResult.VersionCreator);
            Assert.AreEqual(card1Rating, card1FromResult.AverageRating);
            Assert.AreEqual(1, card1FromResult.CountOfUserRatings);
            Assert.IsTrue(card1FromResult.IsInFrench);
            Assert.AreEqual(1, card1FromResult.Tags.Count());
            Assert.AreEqual(searchedTagName, card1FromResult.Tags.Single());
        }
        {
            var card2FromResult = result.Single(card => card.CardId == card2.Id);
            Assert.AreEqual(card2VersionDate, card2FromResult.LastChangeUtcTime);
            Assert.AreEqual(card2.FrontSide, card2FromResult.FrontSide);
            Assert.AreEqual(card2.BackSide, card2FromResult.BackSide);
            Assert.AreEqual(card2.AdditionalInfo, card2FromResult.AdditionalInfo);
            Assert.AreEqual(card2.References, card2FromResult.References);
            Assert.AreEqual(0, card2FromResult.AverageRating);
            Assert.AreEqual(0, card2FromResult.CountOfUserRatings);
            Assert.IsFalse(card2FromResult.IsInFrench);
            Assert.AreEqual(2, card2FromResult.Tags.Count());
        }
    }
}
