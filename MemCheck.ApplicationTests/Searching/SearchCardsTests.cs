using MemCheck.Application.Decks;
using MemCheck.Application.Helpers;
using MemCheck.Application.QueryValidation;
using MemCheck.Application.Ratings;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Searching;

[TestClass()]
public class SearchCardsTests
{
    [TestMethod()]
    public async Task TestEmptyDB_UserNotLoggedIn()
    {
        var testDB = DbHelper.GetEmptyTestDB();

        using var dbContext = new MemCheckDbContext(testDB);
        var result = await new SearchCards(dbContext.AsCallContext()).RunAsync(new SearchCards.Request());
        Assert.AreEqual(0, result.TotalNbCards);
        Assert.AreEqual(0, result.PageCount);
    }
    [TestMethod()]
    public async Task TestEmptyDB_UserLoggedIn()
    {
        var testDB = DbHelper.GetEmptyTestDB();

        var userId = await UserHelper.CreateInDbAsync(testDB);

        using var dbContext = new MemCheckDbContext(testDB);
        var result = await new SearchCards(dbContext.AsCallContext()).RunAsync(new SearchCards.Request { UserId = userId });
        Assert.AreEqual(0, result.TotalNbCards);
        Assert.AreEqual(0, result.PageCount);
    }
    [TestMethod()]
    public async Task TextNotTrimmed_AtStart()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        using var dbContext = new MemCheckDbContext(testDB);
        await Assert.ThrowsExceptionAsync<SearchTextNotTrimmedException>(async () => await new SearchCards(dbContext.AsCallContext()).RunAsync(new SearchCards.Request { RequiredText = ' ' + RandomHelper.String() }));
    }
    [TestMethod()]
    public async Task TextNotTrimmed_AtEnd()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        using var dbContext = new MemCheckDbContext(testDB);
        await Assert.ThrowsExceptionAsync<SearchTextNotTrimmedException>(async () => await new SearchCards(dbContext.AsCallContext()).RunAsync(new SearchCards.Request { RequiredText = RandomHelper.String() + '\n' }));
    }
    [TestMethod()]
    public async Task TestDBWithOnePublicCard_FindAll()
    {
        var testDB = DbHelper.GetEmptyTestDB();

        var user = await UserHelper.CreateUserInDbAsync(testDB);
        var tag1Name = RandomHelper.String();
        var tag1 = await TagHelper.CreateAsync(testDB, user, tag1Name);
        var tag2Name = RandomHelper.String();
        var tag2 = await TagHelper.CreateAsync(testDB, user, tag2Name);
        var card = await CardHelper.CreateAsync(testDB, user.Id, tagIds: new[] { tag1, tag2 });

        using var dbContext = new MemCheckDbContext(testDB);
        var requestWithUser = new SearchCards.Request { UserId = user.Id };
        var resultWithUser = await new SearchCards(dbContext.AsCallContext()).RunAsync(requestWithUser);
        Assert.AreEqual(1, resultWithUser.TotalNbCards);
        Assert.AreEqual(1, resultWithUser.PageCount);
        Assert.AreEqual(card.Id, resultWithUser.Cards.First().CardId);

        var requestWithoutUser = new SearchCards.Request();
        var resultWithoutUser = await new SearchCards(dbContext.AsCallContext()).RunAsync(requestWithoutUser);
        Assert.AreEqual(1, resultWithoutUser.TotalNbCards);
        Assert.AreEqual(1, resultWithoutUser.PageCount);
        var foundCard = resultWithoutUser.Cards.First();
        Assert.AreEqual(card.Id, foundCard.CardId);
        Assert.AreEqual(card.TagsInCards.Count(), foundCard.Tags.Count());
        Assert.IsTrue(foundCard.Tags.Any(t => t == tag1Name));
        Assert.IsTrue(foundCard.Tags.Any(t => t == tag2Name));
        Assert.AreEqual(0, foundCard.CountOfUserRatings);
        Assert.AreEqual(0, foundCard.AverageRating);
        Assert.AreEqual(0, foundCard.CurrentUserRating);
        Assert.IsTrue(!foundCard.DeckInfo.Any());
        Assert.AreEqual(card.FrontSide, foundCard.FrontSide);
        Assert.AreEqual(user.Id, foundCard.VersionCreator.Id);
        Assert.AreEqual(card.VersionDescription, foundCard.VersionDescription);
        Assert.AreEqual(card.VersionUtcDate, foundCard.VersionUtcDate);
        Assert.IsTrue(!foundCard.VisibleTo.Any());
    }
    [TestMethod()]
    public async Task Test_Privacy()
    {
        var testDB = DbHelper.GetEmptyTestDB();

        var user1Id = await UserHelper.CreateInDbAsync(testDB);
        var user2Id = await UserHelper.CreateInDbAsync(testDB);

        var privateCard_User1 = await CardHelper.CreateAsync(testDB, user1Id, userWithViewIds: new[] { user1Id });
        var privateCard_BothUsers = await CardHelper.CreateAsync(testDB, user1Id, userWithViewIds: new[] { user1Id, user2Id });
        var privateCard_User2 = await CardHelper.CreateAsync(testDB, user2Id, userWithViewIds: new[] { user2Id });
        var publicCard = await CardHelper.CreateAsync(testDB, user1Id);

        using var dbContext = new MemCheckDbContext(testDB);
        var resultWithoutUser = await new SearchCards(dbContext.AsCallContext()).RunAsync(new SearchCards.Request());
        Assert.AreEqual(1, resultWithoutUser.TotalNbCards);
        Assert.AreEqual(1, resultWithoutUser.PageCount);
        Assert.AreEqual(publicCard.Id, resultWithoutUser.Cards.First().CardId);

        var resultWithUser1 = await new SearchCards(dbContext.AsCallContext()).RunAsync(new SearchCards.Request { UserId = user1Id });
        Assert.AreEqual(3, resultWithUser1.TotalNbCards);
        Assert.AreEqual(1, resultWithUser1.PageCount);
        Assert.IsTrue(resultWithUser1.Cards.Any(card => card.CardId == privateCard_User1.Id));
        Assert.IsTrue(resultWithUser1.Cards.Any(card => card.CardId == privateCard_BothUsers.Id));
        Assert.IsTrue(resultWithUser1.Cards.Any(card => card.CardId == publicCard.Id));

        var resultWithUser2 = await new SearchCards(dbContext.AsCallContext()).RunAsync(new SearchCards.Request { UserId = user2Id });
        Assert.AreEqual(3, resultWithUser2.TotalNbCards);
        Assert.AreEqual(1, resultWithUser2.PageCount);
        Assert.IsTrue(resultWithUser2.Cards.Any(card => card.CardId == privateCard_BothUsers.Id));
        Assert.IsTrue(resultWithUser2.Cards.Any(card => card.CardId == privateCard_User2.Id));
        Assert.IsTrue(resultWithUser2.Cards.Any(card => card.CardId == publicCard.Id));
    }
    [TestMethod()]
    public async Task Test_Deck_Inclusive()
    {
        var testDB = DbHelper.GetEmptyTestDB();

        var user1Id = await UserHelper.CreateInDbAsync(testDB);
        var user2Id = await UserHelper.CreateInDbAsync(testDB);

        var card1 = await CardHelper.CreateAsync(testDB, user1Id);
        var card2 = await CardHelper.CreateAsync(testDB, user1Id);
        await CardHelper.CreateAsync(testDB, user1Id);

        var user1Emptydeck = await DeckHelper.CreateAsync(testDB, user1Id);
        var user1deck = await DeckHelper.CreateAsync(testDB, user1Id);

        using (var dbContext = new MemCheckDbContext(testDB))
            await new AddCardsInDeck(dbContext.AsCallContext()).RunAsync(new AddCardsInDeck.Request(user1Id, user1deck, new[] { card1.Id, card2.Id }));

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            Assert.AreEqual(0, (await new SearchCards(dbContext.AsCallContext()).RunAsync(new SearchCards.Request { UserId = user1Id, Deck = user1Emptydeck })).TotalNbCards);

            var resultOnDeck = await new SearchCards(dbContext.AsCallContext()).RunAsync(new SearchCards.Request { UserId = user1Id, Deck = user1deck });
            Assert.AreEqual(2, resultOnDeck.TotalNbCards);
            Assert.IsTrue(resultOnDeck.Cards.Any(card => card.CardId == card1.Id));
            Assert.IsTrue(resultOnDeck.Cards.Any(card => card.CardId == card2.Id));

            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new SearchCards(dbContext.AsCallContext()).RunAsync(new SearchCards.Request { Deck = user1Emptydeck }));

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new SearchCards(dbContext.AsCallContext()).RunAsync(new SearchCards.Request { UserId = user2Id, Deck = user1deck }));
        }
    }
    [TestMethod()]
    public async Task Test_Deck_Exclusive()
    {
        var testDB = DbHelper.GetEmptyTestDB();

        var user1Id = await UserHelper.CreateInDbAsync(testDB);
        var user2Id = await UserHelper.CreateInDbAsync(testDB);

        var card1 = await CardHelper.CreateAsync(testDB, user1Id);
        var card2 = await CardHelper.CreateAsync(testDB, user1Id);
        var card3 = await CardHelper.CreateAsync(testDB, user1Id);

        var user1Emptydeck = await DeckHelper.CreateAsync(testDB, user1Id);
        var user1deck = await DeckHelper.CreateAsync(testDB, user1Id);

        using (var dbContext = new MemCheckDbContext(testDB))
            await new AddCardsInDeck(dbContext.AsCallContext()).RunAsync(new AddCardsInDeck.Request(user1Id, user1deck, new[] { card1.Id, card2.Id }));

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            Assert.AreEqual(3, (await new SearchCards(dbContext.AsCallContext()).RunAsync(new SearchCards.Request { UserId = user1Id, Deck = user1Emptydeck, DeckIsInclusive = false })).TotalNbCards);

            var resultOnDeck = await new SearchCards(dbContext.AsCallContext()).RunAsync(new SearchCards.Request { UserId = user1Id, Deck = user1deck, DeckIsInclusive = false });
            Assert.AreEqual(1, resultOnDeck.TotalNbCards);
            Assert.IsTrue(resultOnDeck.Cards.Any(card => card.CardId == card3.Id));

            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new SearchCards(dbContext.AsCallContext()).RunAsync(new SearchCards.Request { Deck = user1Emptydeck, DeckIsInclusive = false }));

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new SearchCards(dbContext.AsCallContext()).RunAsync(new SearchCards.Request { UserId = user2Id, Deck = user1deck, DeckIsInclusive = false }));
        }
    }
    [TestMethod()]
    public async Task Test_AfterDate()
    {
        var testDB = DbHelper.GetEmptyTestDB();

        var userId = await UserHelper.CreateInDbAsync(testDB);
        await CardHelper.CreateAsync(testDB, userId, new DateTime(2040, 1, 5));
        await CardHelper.CreateAsync(testDB, userId, new DateTime(2040, 1, 7));

        using var dbContext = new MemCheckDbContext(testDB);
        var result1Jan = await new SearchCards(dbContext.AsCallContext()).RunAsync(new SearchCards.Request { MinimumUtcDateOfCards = new DateTime(2040, 1, 1) });
        Assert.AreEqual(2, result1Jan.TotalNbCards);

        var result5Jan = await new SearchCards(dbContext.AsCallContext()).RunAsync(new SearchCards.Request { MinimumUtcDateOfCards = new DateTime(2040, 1, 5) });
        Assert.AreEqual(2, result5Jan.TotalNbCards);

        var result6Jan = await new SearchCards(dbContext.AsCallContext()).RunAsync(new SearchCards.Request { MinimumUtcDateOfCards = new DateTime(2040, 1, 6) });
        Assert.AreEqual(1, result6Jan.TotalNbCards);

        var result7Jan = await new SearchCards(dbContext.AsCallContext()).RunAsync(new SearchCards.Request { MinimumUtcDateOfCards = new DateTime(2040, 1, 7) });
        Assert.AreEqual(1, result7Jan.TotalNbCards);

        var result8Jan = await new SearchCards(dbContext.AsCallContext()).RunAsync(new SearchCards.Request { MinimumUtcDateOfCards = new DateTime(2040, 1, 8) });
        Assert.AreEqual(0, result8Jan.TotalNbCards);
    }
    [TestMethod()]
    public async Task Rating_SingleCard_SingleUser()
    {
        var testDB = DbHelper.GetEmptyTestDB();

        var userId = await UserHelper.CreateInDbAsync(testDB);
        var cardId = await CardHelper.CreateIdAsync(testDB, userId);

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var result = await new SearchCards(dbContext.AsCallContext()).RunAsync(new SearchCards.Request { UserId = userId, RatingFiltering = SearchCards.Request.RatingFilteringMode.NoRating });
            Assert.AreEqual(1, result.TotalNbCards);
            Assert.AreEqual(0, result.Cards.Single().CurrentUserRating);
            Assert.AreEqual(0, result.Cards.Single().CountOfUserRatings);
            Assert.AreEqual(0, result.Cards.Single().AverageRating);
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var result = await new SearchCards(dbContext.AsCallContext()).RunAsync(new SearchCards.Request { UserId = userId, RatingFiltering = SearchCards.Request.RatingFilteringMode.AtLeast, RatingFilteringValue = 3 });
            Assert.AreEqual(0, result.TotalNbCards);
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var result = await new SearchCards(dbContext.AsCallContext()).RunAsync(new SearchCards.Request { UserId = userId, RatingFiltering = SearchCards.Request.RatingFilteringMode.AtMost, RatingFilteringValue = 3 });
            Assert.AreEqual(0, result.TotalNbCards);
        }

        using (var dbContext = new MemCheckDbContext(testDB))
            await new SetCardRating(dbContext.AsCallContext()).RunAsync(new SetCardRating.Request(userId, cardId, 4));

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var result = await new SearchCards(dbContext.AsCallContext()).RunAsync(new SearchCards.Request { UserId = userId, RatingFiltering = SearchCards.Request.RatingFilteringMode.NoRating });
            Assert.AreEqual(0, result.TotalNbCards);
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var result = await new SearchCards(dbContext.AsCallContext()).RunAsync(new SearchCards.Request { UserId = userId, RatingFiltering = SearchCards.Request.RatingFilteringMode.AtLeast, RatingFilteringValue = 3 });
            Assert.AreEqual(1, result.TotalNbCards);
            Assert.AreEqual(4, result.Cards.Single().CurrentUserRating);
            Assert.AreEqual(1, result.Cards.Single().CountOfUserRatings);
            Assert.AreEqual(4, result.Cards.Single().AverageRating);
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var result = await new SearchCards(dbContext.AsCallContext()).RunAsync(new SearchCards.Request { UserId = userId, RatingFiltering = SearchCards.Request.RatingFilteringMode.AtLeast, RatingFilteringValue = 4 });
            Assert.AreEqual(1, result.TotalNbCards);
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var result = await new SearchCards(dbContext.AsCallContext()).RunAsync(new SearchCards.Request { UserId = userId, RatingFiltering = SearchCards.Request.RatingFilteringMode.AtLeast, RatingFilteringValue = 5 });
            Assert.AreEqual(0, result.TotalNbCards);
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var result = await new SearchCards(dbContext.AsCallContext()).RunAsync(new SearchCards.Request { UserId = userId, RatingFiltering = SearchCards.Request.RatingFilteringMode.AtMost, RatingFilteringValue = 3 });
            Assert.AreEqual(0, result.TotalNbCards);
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var result = await new SearchCards(dbContext.AsCallContext()).RunAsync(new SearchCards.Request { UserId = userId, RatingFiltering = SearchCards.Request.RatingFilteringMode.AtMost, RatingFilteringValue = 4 });
            Assert.AreEqual(1, result.TotalNbCards);
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var result = await new SearchCards(dbContext.AsCallContext()).RunAsync(new SearchCards.Request { UserId = userId, RatingFiltering = SearchCards.Request.RatingFilteringMode.AtMost, RatingFilteringValue = 5 });
            Assert.AreEqual(1, result.TotalNbCards);
        }
    }
    [TestMethod()]
    public async Task Rating_SingleCard_SearchByOtherUser()
    {
        var testDB = DbHelper.GetEmptyTestDB();

        var userId = await UserHelper.CreateInDbAsync(testDB);
        var cardId = await CardHelper.CreateIdAsync(testDB, userId);

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var result = await new SearchCards(dbContext.AsCallContext()).RunAsync(new SearchCards.Request { RatingFiltering = SearchCards.Request.RatingFilteringMode.NoRating });
            Assert.AreEqual(1, result.TotalNbCards);
            Assert.AreEqual(0, result.Cards.Single().CurrentUserRating);
            Assert.AreEqual(0, result.Cards.Single().CountOfUserRatings);
            Assert.AreEqual(0, result.Cards.Single().AverageRating);
        }

        using (var dbContext = new MemCheckDbContext(testDB))
            await new SetCardRating(dbContext.AsCallContext()).RunAsync(new SetCardRating.Request(userId, cardId, 4));

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var result = await new SearchCards(dbContext.AsCallContext()).RunAsync(new SearchCards.Request { RatingFiltering = SearchCards.Request.RatingFilteringMode.NoRating });
            Assert.AreEqual(0, result.TotalNbCards);
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var result = await new SearchCards(dbContext.AsCallContext()).RunAsync(new SearchCards.Request { RatingFiltering = SearchCards.Request.RatingFilteringMode.AtLeast, RatingFilteringValue = 3 });
            Assert.AreEqual(1, result.TotalNbCards);
            Assert.AreEqual(0, result.Cards.Single().CurrentUserRating);
            Assert.AreEqual(1, result.Cards.Single().CountOfUserRatings);
            Assert.AreEqual(4, result.Cards.Single().AverageRating);
        }
    }
    [TestMethod()]
    public async Task Rating_SingleCard_TwoUsers()
    {
        var testDB = DbHelper.GetEmptyTestDB();

        var user1Id = await UserHelper.CreateInDbAsync(testDB);
        var user2Id = await UserHelper.CreateInDbAsync(testDB);
        var cardId = await CardHelper.CreateIdAsync(testDB, user1Id);

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            await new SetCardRating(dbContext.AsCallContext()).RunAsync(new SetCardRating.Request(user1Id, cardId, 4));
            await new SetCardRating(dbContext.AsCallContext()).RunAsync(new SetCardRating.Request(user2Id, cardId, 2));
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var result = await new SearchCards(dbContext.AsCallContext()).RunAsync(new SearchCards.Request { UserId = user1Id, RatingFiltering = SearchCards.Request.RatingFilteringMode.AtLeast, RatingFilteringValue = 3 });
            Assert.AreEqual(1, result.TotalNbCards);
            Assert.AreEqual(4, result.Cards.Single().CurrentUserRating);
            Assert.AreEqual(2, result.Cards.Single().CountOfUserRatings);
            Assert.AreEqual(3, result.Cards.Single().AverageRating);
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var result = await new SearchCards(dbContext.AsCallContext()).RunAsync(new SearchCards.Request { UserId = user2Id, RatingFiltering = SearchCards.Request.RatingFilteringMode.AtLeast, RatingFilteringValue = 3 });
            Assert.AreEqual(1, result.TotalNbCards);
            Assert.AreEqual(2, result.Cards.Single().CurrentUserRating);
            Assert.AreEqual(2, result.Cards.Single().CountOfUserRatings);
            Assert.AreEqual(3, result.Cards.Single().AverageRating);
        }

    }
    [TestMethod()]
    public async Task Rating_TwoCards_TwoUsers()
    {
        var testDB = DbHelper.GetEmptyTestDB();

        var user1Id = await UserHelper.CreateInDbAsync(testDB);
        var user2Id = await UserHelper.CreateInDbAsync(testDB);
        var card1Id = await CardHelper.CreateIdAsync(testDB, user1Id);
        var card2Id = await CardHelper.CreateIdAsync(testDB, user2Id);

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var result = await new SearchCards(dbContext.AsCallContext()).RunAsync(new SearchCards.Request { UserId = user1Id, RatingFiltering = SearchCards.Request.RatingFilteringMode.AtLeast, RatingFilteringValue = 3 });
            Assert.AreEqual(0, result.TotalNbCards);
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            await new SetCardRating(dbContext.AsCallContext()).RunAsync(new SetCardRating.Request(user1Id, card1Id, 4));
            await new SetCardRating(dbContext.AsCallContext()).RunAsync(new SetCardRating.Request(user2Id, card1Id, 2));
            await new SetCardRating(dbContext.AsCallContext()).RunAsync(new SetCardRating.Request(user1Id, card2Id, 5));
            await new SetCardRating(dbContext.AsCallContext()).RunAsync(new SetCardRating.Request(user2Id, card2Id, 3));
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var result = await new SearchCards(dbContext.AsCallContext()).RunAsync(new SearchCards.Request { UserId = user1Id, RatingFiltering = SearchCards.Request.RatingFilteringMode.AtLeast, RatingFilteringValue = 3 });
            Assert.AreEqual(2, result.TotalNbCards);
            Assert.AreEqual(4, result.Cards.Single(c => c.CardId == card1Id).CurrentUserRating);
            Assert.AreEqual(5, result.Cards.Single(c => c.CardId == card2Id).CurrentUserRating);
            Assert.AreEqual(2, result.Cards.Single(c => c.CardId == card1Id).CountOfUserRatings);
            Assert.AreEqual(2, result.Cards.Single(c => c.CardId == card2Id).CountOfUserRatings);
            Assert.AreEqual(3, result.Cards.Single(c => c.CardId == card1Id).AverageRating);
            Assert.AreEqual(4, result.Cards.Single(c => c.CardId == card2Id).AverageRating);
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var result = await new SearchCards(dbContext.AsCallContext()).RunAsync(new SearchCards.Request { UserId = user1Id, RatingFiltering = SearchCards.Request.RatingFilteringMode.AtLeast, RatingFilteringValue = 4 });
            Assert.AreEqual(1, result.TotalNbCards);
            Assert.AreEqual(card2Id, result.Cards.Single().CardId);
            Assert.AreEqual(5, result.Cards.Single().CurrentUserRating);
            Assert.AreEqual(2, result.Cards.Single().CountOfUserRatings);
            Assert.AreEqual(4, result.Cards.Single().AverageRating);
        }
    }
    [TestMethod()]
    public async Task TestFindNoWithRef()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(testDB);
        await CardHelper.CreateAsync(testDB, userId);
        await CardHelper.CreateAsync(testDB, userId);

        using var dbContext = new MemCheckDbContext(testDB);
        var request = new SearchCards.Request { RequiredText = RandomHelper.String() };
        var result = await new SearchCards(dbContext.AsCallContext()).RunAsync(request);
        Assert.AreEqual(0, result.TotalNbCards);
    }
    [TestMethod()]
    public async Task TestFindOneCardWithRef()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(testDB);
        var searchedString = RandomHelper.String();
        await CardHelper.CreateAsync(testDB, userId);
        var cardWithRef = await CardHelper.CreateAsync(testDB, userId, references: RandomHelper.String() + searchedString + RandomHelper.String());
        await CardHelper.CreateAsync(testDB, userId);

        using var dbContext = new MemCheckDbContext(testDB);
        var request = new SearchCards.Request { RequiredText = searchedString };
        var result = await new SearchCards(dbContext.AsCallContext()).RunAsync(request);
        Assert.AreEqual(1, result.TotalNbCards);
        Assert.AreEqual(cardWithRef.Id, result.Cards.Single().CardId);
    }
    [TestMethod()]
    public async Task TestFindTwoCardsWithRef()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(testDB);
        var searchedString = RandomHelper.String();
        var card1 = await CardHelper.CreateAsync(testDB, userId, references: RandomHelper.String() + searchedString + RandomHelper.String());
        await CardHelper.CreateAsync(testDB, userId);
        await CardHelper.CreateAsync(testDB, userId);
        await CardHelper.CreateAsync(testDB, userId);
        var card2 = await CardHelper.CreateAsync(testDB, userId, references: searchedString);

        using var dbContext = new MemCheckDbContext(testDB);
        var request = new SearchCards.Request { RequiredText = searchedString };
        var result = await new SearchCards(dbContext.AsCallContext()).RunAsync(request);
        Assert.AreEqual(2, result.TotalNbCards);
        Assert.IsNotNull(result.Cards.SingleOrDefault(card => card.CardId == card1.Id));
        Assert.IsNotNull(result.Cards.SingleOrDefault(card => card.CardId == card2.Id));
    }
    [TestMethod()]
    public async Task TestResultContainsUserWithView()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var user1 = await UserHelper.CreateUserInDbAsync(testDB);
        var user2 = await UserHelper.CreateUserInDbAsync(testDB);
        await CardHelper.CreateIdAsync(testDB, user1.Id, userWithViewIds: new[] { user1.Id, user2.Id });

        using var dbContext = new MemCheckDbContext(testDB);
        var request = new SearchCards.Request { UserId = user2.Id };
        var result = await new SearchCards(dbContext.AsCallContext()).RunAsync(request);
        Assert.AreEqual(1, result.TotalNbCards);
        var card = result.Cards.Single();
        Assert.AreEqual(2, card.VisibleTo.Count());
        var visibleToUser1 = card.VisibleTo.Single(visibleTo => visibleTo.UserId == user1.Id);
        Assert.IsNotNull(visibleToUser1.User);
        Assert.AreEqual(user1.UserName, visibleToUser1.User.UserName);
        var visibleToUser2 = card.VisibleTo.Single(visibleTo => visibleTo.UserId == user2.Id);
        Assert.IsNotNull(visibleToUser2.User);
        Assert.AreEqual(user2.UserName, visibleToUser2.User.UserName);
    }
    [TestMethod()]
    public async Task TestFind_ReferenceNotEmpty()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(testDB);
        await CardHelper.CreateAsync(testDB, userId, references: "");
        var cardWithRef = await CardHelper.CreateAsync(testDB, userId, references: RandomHelper.String());

        using var dbContext = new MemCheckDbContext(testDB);
        var request = new SearchCards.Request { Reference = SearchCards.Request.ReferenceFiltering.NotEmpty };
        var result = await new SearchCards(dbContext.AsCallContext()).RunAsync(request);
        Assert.AreEqual(1, result.TotalNbCards);
        Assert.AreEqual(cardWithRef.Id, result.Cards.Single().CardId);
    }
    [TestMethod()]
    public async Task TestFind_ReferenceNone()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(testDB);
        var cardWithoutRef = await CardHelper.CreateAsync(testDB, userId, references: "");
        await CardHelper.CreateAsync(testDB, userId, references: RandomHelper.String());

        using var dbContext = new MemCheckDbContext(testDB);
        var request = new SearchCards.Request { Reference = SearchCards.Request.ReferenceFiltering.None };
        var result = await new SearchCards(dbContext.AsCallContext()).RunAsync(request);
        Assert.AreEqual(1, result.TotalNbCards);
        Assert.AreEqual(cardWithoutRef.Id, result.Cards.Single().CardId);
    }
    [TestMethod()]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Really want to use lower case for test")]
    public async Task TestCaseInsensitive()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(testDB);
        var searchedString = RandomHelper.String().ToUpperInvariant() + RandomHelper.String().ToLowerInvariant(); //searchedString contains both upper and lower case chars

        var cardWithSameCasingInText = await CardHelper.CreateAsync(testDB, userId, frontSide: RandomHelper.String() + searchedString + RandomHelper.String());
        var cardWithUpperCasingInText = await CardHelper.CreateAsync(testDB, userId, backSide: RandomHelper.String() + searchedString.ToUpperInvariant() + RandomHelper.String());
        var cardWithLowerCasingInText = await CardHelper.CreateAsync(testDB, userId, references: RandomHelper.String() + searchedString.ToLowerInvariant() + RandomHelper.String());

        //Three cards not to be found...
        await CardHelper.CreateAsync(testDB, userId);
        await CardHelper.CreateAsync(testDB, userId);
        await CardHelper.CreateAsync(testDB, userId);

        using var dbContext = new MemCheckDbContext(testDB);
        var request = new SearchCards.Request { RequiredText = searchedString };
        var result = await new SearchCards(dbContext.AsCallContext()).RunAsync(request);

        Assert.AreEqual(3, result.TotalNbCards);
        Assert.IsTrue(result.Cards.Any(card => card.CardId == cardWithSameCasingInText.Id));
        Assert.IsTrue(result.Cards.Any(card => card.CardId == cardWithUpperCasingInText.Id));
        Assert.IsTrue(result.Cards.Any(card => card.CardId == cardWithLowerCasingInText.Id));
    }
}
