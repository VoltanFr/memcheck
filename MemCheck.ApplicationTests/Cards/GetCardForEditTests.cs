using MemCheck.Application.Tests.Helpers;
using MemCheck.Basics;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards
{
    [TestClass()]
    public class GetCardForEditTests
    {
        [TestMethod()]
        public async Task UserNotLoggedIn()
        {
            using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetCardForEdit(dbContext.AsCallContext()).RunAsync(new GetCardForEdit.Request(Guid.Empty, Guid.Empty)));
        }
        [TestMethod()]
        public async Task UserDoesNotExist()
        {
            using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetCardForEdit(dbContext.AsCallContext()).RunAsync(new GetCardForEdit.Request(Guid.NewGuid(), Guid.Empty)));
        }
        [TestMethod()]
        public async Task CardDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(db);
            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetCardForEdit(dbContext.AsCallContext()).RunAsync(new GetCardForEdit.Request(userId, Guid.NewGuid())));
        }
        [TestMethod()]
        public async Task FailIfUserCanNotView()
        {
            var db = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(db);
            var language = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, userId, language: language, userWithViewIds: userId.AsArray());
            var otherUserId = await UserHelper.CreateInDbAsync(db);
            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetCardForEdit(dbContext.AsCallContext()).RunAsync(new GetCardForEdit.Request(otherUserId, card.Id)));
        }
        [TestMethod()]
        public async Task CardWithPreviousVersion()
        {
            var db = DbHelper.GetEmptyTestDB();
            var language = await CardLanguagHelper.CreateAsync(db);

            var firstVersionCreatorId = await UserHelper.CreateInDbAsync(db);
            var firstVersionDate = RandomHelper.Date();
            var card = await CardHelper.CreateAsync(db, firstVersionCreatorId, language: language, versionDate: firstVersionDate);

            var lastVersionCreatorName = RandomHelper.String();
            var lastVersionCreatorId = await UserHelper.CreateInDbAsync(db, userName: lastVersionCreatorName);
            var lastVersionDate = RandomHelper.Date();
            var lastVersionDescription = RandomHelper.String();
            var lastVersionFrontSide = RandomHelper.String();
            using (var dbContext = new MemCheckDbContext(db))
                await new UpdateCard(dbContext.AsCallContext(), lastVersionDate).RunAsync(UpdateCardHelper.RequestForFrontSideChange(card, lastVersionFrontSide, versionCreator: lastVersionCreatorId, versionDescription: lastVersionDescription));

            var otherUserId = await UserHelper.CreateInDbAsync(db);

            using (var dbContext = new MemCheckDbContext(db))
            {
                var loaded = await new GetCardForEdit(dbContext.AsCallContext()).RunAsync(new GetCardForEdit.Request(otherUserId, card.Id));

                Assert.AreEqual(firstVersionDate, loaded.FirstVersionUtcDate);
                Assert.AreEqual(lastVersionDate, loaded.LastVersionUtcDate);
                Assert.AreEqual(lastVersionCreatorName, loaded.LastVersionCreatorName);
                Assert.AreEqual(lastVersionDescription, loaded.LastVersionDescription);
                Assert.AreEqual(lastVersionFrontSide, loaded.FrontSide);
            }
        }
        [TestMethod()]
        public async Task CheckAllFields()
        {
            var db = DbHelper.GetEmptyTestDB();
            var languageName = RandomHelper.String();
            var language = await CardLanguagHelper.CreateAsync(db, languageName);
            var creatorName = RandomHelper.String();
            var creatorId = await UserHelper.CreateInDbAsync(db, userName: creatorName);
            var creationDate = RandomHelper.Date();
            var frontSide = RandomHelper.String();
            var backSide = RandomHelper.String();
            var additionalInfo = RandomHelper.String();
            var tagName = RandomHelper.String();
            var tag = await TagHelper.CreateAsync(db, tagName);
            var otherUserName = RandomHelper.String();
            var otherUserId = await UserHelper.CreateInDbAsync(db, userName: otherUserName);
            var versionDescription = RandomHelper.String();
            var card = await CardHelper.CreateAsync(db, creatorId, language: language, versionDate: creationDate, frontSide: frontSide, backSide: backSide, additionalInfo: additionalInfo, tagIds: tag.AsArray(), userWithViewIds: new[] { creatorId, otherUserId }, versionDescription: versionDescription);

            var deck = await DeckHelper.CreateAsync(db, otherUserId);
            await DeckHelper.AddCardAsync(db, deck, card.Id);

            using var dbContext = new MemCheckDbContext(db);
            var loaded = await new GetCardForEdit(dbContext.AsCallContext()).RunAsync(new GetCardForEdit.Request(creatorId, card.Id));

            Assert.AreEqual(frontSide, loaded.FrontSide);
            Assert.AreEqual(backSide, loaded.BackSide);
            Assert.AreEqual(additionalInfo, loaded.AdditionalInfo);
            Assert.AreEqual(language, loaded.LanguageId);
            Assert.AreEqual(languageName, loaded.LanguageName);
            Assert.AreEqual(tag, loaded.Tags.Single().TagId);
            Assert.AreEqual(tagName, loaded.Tags.Single().TagName);
            Assert.AreEqual(2, loaded.UsersWithVisibility.Count());
            Assert.IsTrue(loaded.UsersWithVisibility.Count(u => u.UserId == creatorId) == 1);
            Assert.AreEqual(creatorName, loaded.UsersWithVisibility.Single(u => u.UserId == creatorId).UserName);
            Assert.IsTrue(loaded.UsersWithVisibility.Count(u => u.UserId == otherUserId) == 1);
            Assert.AreEqual(otherUserName, loaded.UsersWithVisibility.Single(u => u.UserId == otherUserId).UserName);
            Assert.AreEqual(creationDate, loaded.FirstVersionUtcDate);
            Assert.AreEqual(creationDate, loaded.LastVersionUtcDate);
            Assert.AreEqual(creatorName, loaded.LastVersionCreatorName);
            Assert.AreEqual(versionDescription, loaded.LastVersionDescription);
            Assert.AreEqual(1, loaded.UsersOwningDeckIncluding.Count());
            Assert.IsTrue(loaded.UsersOwningDeckIncluding.Single() == otherUserName);
            Assert.AreEqual(0, loaded.Images.Count());
            Assert.AreEqual(0, loaded.UserRating);
            Assert.AreEqual(0, loaded.AverageRating);
            Assert.AreEqual(0, loaded.CountOfUserRatings);
        }
    }
}
