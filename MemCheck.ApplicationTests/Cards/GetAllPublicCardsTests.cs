using MemCheck.Application.Helpers;
using MemCheck.Basics;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards;

[TestClass()]
public class GetAllPublicCardsTests
{
    [TestMethod()]
    public async Task EmptyDb()
    {
        var db = DbHelper.GetEmptyTestDB();
        using var dbContext = new MemCheckDbContext(db);
        var result = await new GetAllPublicCards(dbContext.AsCallContext()).RunAsync(new GetAllPublicCards.Request());
        Assert.AreEqual(0, result.Cards.Length);
    }
    [TestMethod()]
    public async Task SingleCardVisibleToSingleUser()
    {
        var db = DbHelper.GetEmptyTestDB();
        var cardCreator = await UserHelper.CreateInDbAsync(db);
        await CardHelper.CreateIdAsync(db, cardCreator, userWithViewIds: cardCreator.AsArray());

        using var dbContext = new MemCheckDbContext(db);
        var result = await new GetAllPublicCards(dbContext.AsCallContext()).RunAsync(new GetAllPublicCards.Request());
        Assert.AreEqual(0, result.Cards.Length);
    }
    [TestMethod()]
    public async Task SingleCardVisibleToSeveralUsers()
    {
        var db = DbHelper.GetEmptyTestDB();
        var cardCreator = await UserHelper.CreateInDbAsync(db);
        var otherUser = await UserHelper.CreateInDbAsync(db);
        await CardHelper.CreateIdAsync(db, cardCreator, userWithViewIds: new[] { cardCreator, otherUser });

        using var dbContext = new MemCheckDbContext(db);
        var result = await new GetAllPublicCards(dbContext.AsCallContext()).RunAsync(new GetAllPublicCards.Request());
        Assert.AreEqual(0, result.Cards.Length);
    }
    [TestMethod()]
    public async Task SinglePublicCard()
    {
        var db = DbHelper.GetEmptyTestDB();
        var cardCreator = await UserHelper.CreateInDbAsync(db);
        var frontSide = RandomHelper.String();
        var backSide = RandomHelper.String();
        var versionDate = RandomHelper.Date();
        var cardId = await CardHelper.CreateIdAsync(db, cardCreator, versionDate, frontSide: frontSide, backSide: backSide);
        var rating = RandomHelper.Rating();
        await RatingHelper.RecordForUserAsync(db, cardCreator, cardId, rating);

        using var dbContext = new MemCheckDbContext(db);
        var result = await new GetAllPublicCards(dbContext.AsCallContext()).RunAsync(new GetAllPublicCards.Request());
        Assert.AreEqual(1, result.Cards.Length);
        var resultCard = result.Cards.Single();
        Assert.AreEqual(frontSide, resultCard.FrontSide);
        Assert.AreEqual(backSide, resultCard.BackSide);
        Assert.AreEqual(rating, resultCard.AverageRating);
    }
    [TestMethod()]
    public async Task ComplexCase()
    {
        var db = DbHelper.GetEmptyTestDB();

        var user1 = await UserHelper.CreateInDbAsync(db);
        var user2 = await UserHelper.CreateInDbAsync(db);
        var user3 = await UserHelper.CreateInDbAsync(db);

        var card1FrontSide = RandomHelper.String();
        var card1BackSide = RandomHelper.String();
        var card1VersionDate = RandomHelper.Date();
        var card1Id = await CardHelper.CreateIdAsync(db, user1, card1VersionDate, frontSide: card1FrontSide, backSide: card1BackSide);
        await RatingHelper.RecordForUserAsync(db, user1, card1Id, 2);
        await RatingHelper.RecordForUserAsync(db, user3, card1Id, 4);

        var card2BackSide = RandomHelper.String();
        var card2CreationDate = RandomHelper.Date();
        var card2 = await CardHelper.CreateAsync(db, user1, card2CreationDate, backSide: card2BackSide);
        await RatingHelper.RecordForUserAsync(db, user3, card2.Id, 5);
        var card2FrontSide = RandomHelper.String();
        var card2VersionDate = RandomHelper.Date(card2CreationDate);
        await UpdateCardHelper.RunAsync(db, UpdateCardHelper.RequestForFrontSideChange(card2, card2FrontSide, user3));
        await RatingHelper.RecordForUserAsync(db, user3, card2.Id, 5);

        await CardHelper.CreateIdAsync(db, user1, userWithViewIds: new[] { user1, user3 });

        using var dbContext = new MemCheckDbContext(db);
        var result = await new GetAllPublicCards(dbContext.AsCallContext()).RunAsync(new GetAllPublicCards.Request());
        Assert.AreEqual(2, result.Cards.Length);

        {
            var resultCard1 = result.Cards.Single(card => card.CardId == card1Id);
            Assert.AreEqual(card1FrontSide, resultCard1.FrontSide);
            Assert.AreEqual(card1BackSide, resultCard1.BackSide);
            Assert.AreEqual(3, resultCard1.AverageRating);
        }

        {
            var resultCard2 = result.Cards.Single(card => card.CardId == card2.Id);
            Assert.AreEqual(card2FrontSide, resultCard2.FrontSide);
            Assert.AreEqual(card2BackSide, resultCard2.BackSide);
            Assert.AreEqual(5, resultCard2.AverageRating);
        }
    }
}
