using MemCheck.Application.Cards;
using MemCheck.Application.Helpers;
using MemCheck.Basics;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.History;

[TestClass()]
public class GetCardVersionsTests
{
    [TestMethod()]
    public async Task UserNotLoggedIn()
    {
        using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetCardVersions(dbContext.AsCallContext()).RunAsync(new GetCardVersions.Request(Guid.Empty, Guid.Empty)));
    }
    [TestMethod()]
    public async Task UserDoesNotExist()
    {
        using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetCardVersions(dbContext.AsCallContext()).RunAsync(new GetCardVersions.Request(Guid.NewGuid(), Guid.Empty)));
    }
    [TestMethod()]
    public async Task CardDoesNotExist()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetCardVersions(dbContext.AsCallContext()).RunAsync(new GetCardVersions.Request(userId, Guid.NewGuid())));
    }
    [TestMethod()]
    public async Task FailIfUserCanNotViewCurrentVersion()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var language = await CardLanguageHelper.CreateAsync(db);
        var card = await CardHelper.CreateAsync(db, userId, language: language);    //Created public
        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateCard(dbContext.AsCallContext()).RunAsync(UpdateCardHelper.RequestForVisibilityChange(card, userWithViewIds: userId.AsArray()));    //Now private
        var otherUserId = await UserHelper.CreateInDbAsync(db);
        using (var dbContext = new MemCheckDbContext(db))
        {
            var versions = await new GetCardVersions(dbContext.AsCallContext()).RunAsync(new GetCardVersions.Request(userId, card.Id));
            Assert.AreEqual(2, versions.Count());
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetCardVersions(dbContext.AsCallContext()).RunAsync(new GetCardVersions.Request(otherUserId, card.Id)));
        }
    }
    [TestMethod()]
    public async Task SingleVersion()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userName = RandomHelper.String();
        var userId = await UserHelper.CreateInDbAsync(db, userName: userName);
        var versionDate = RandomHelper.Date();
        var versionDescription = RandomHelper.String();
        var card = await CardHelper.CreateAsync(db, userId, versionDate, frontSide: "", backSide: "", additionalInfo: "", references: "", versionDescription: versionDescription);

        using var dbContext = new MemCheckDbContext(db);
        var versions = await new GetCardVersions(dbContext.AsCallContext()).RunAsync(new GetCardVersions.Request(userId, card.Id));
        Assert.AreEqual(1, versions.Count());

        var version = versions.Single();

        Assert.AreEqual(userName, version.VersionCreator);
        Assert.AreEqual(versionDate, version.VersionUtcDate);
        Assert.AreEqual(versionDescription, version.VersionDescription);
        Assert.AreEqual(2, version.ChangedFieldNames.Count());
        CollectionAssert.Contains(version.ChangedFieldNames.ToList(), GetCardVersions.LanguageName);
        CollectionAssert.Contains(version.ChangedFieldNames.ToList(), GetCardVersions.UsersWithView);
    }
    [TestMethod()]
    public async Task TwoVersions()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        var initialVersionDate = RandomHelper.Date();
        var initialVersionDescription = RandomHelper.String();
        var card = await CardHelper.CreateAsync(db, userId, versionDate: initialVersionDate, versionDescription: initialVersionDescription);

        var lastVersionDate = RandomHelper.Date();
        var lastVersionDescription = RandomHelper.String();
        using (var dbContext = new MemCheckDbContext(db))
        {
            var request = UpdateCardHelper.RequestForReferencesChange(card, RandomHelper.String(), versionDescription: lastVersionDescription);
            await new UpdateCard(dbContext.AsCallContext(), lastVersionDate).RunAsync(request);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var versions = await new GetCardVersions(dbContext.AsCallContext()).RunAsync(new GetCardVersions.Request(userId, card.Id));

            Assert.AreEqual(2, versions.Count());

            var initialVersion = versions.Last();
            Assert.IsNotNull(initialVersion.VersionId);
            Assert.AreEqual(initialVersionDate, initialVersion.VersionUtcDate);
            Assert.AreEqual(initialVersionDescription, initialVersion.VersionDescription);

            var lastVersion = versions.First();
            Assert.IsNull(lastVersion.VersionId);
            Assert.AreEqual(lastVersionDate, lastVersion.VersionUtcDate);
            Assert.AreEqual(lastVersionDescription, lastVersion.VersionDescription);
            Assert.AreEqual(1, lastVersion.ChangedFieldNames.Count());
            Assert.AreEqual(GetCardVersions.References, lastVersion.ChangedFieldNames.Single());
        }
    }
    [TestMethod()]
    public async Task ThreeVersions()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userName = RandomHelper.String();
        var userId = await UserHelper.CreateInDbAsync(db, userName: userName);
        var language = await CardLanguageHelper.CreateAsync(db);
        var oldestDate = RandomHelper.Date();
        var oldestDescription = RandomHelper.String();
        var card = await CardHelper.CreateAsync(db, userId, language: language, versionDate: oldestDate, versionDescription: oldestDescription);

        var otherUserName = RandomHelper.String();
        var otherUserId = await UserHelper.CreateInDbAsync(db, userName: otherUserName);
        var intermediaryDate = RandomHelper.Date();
        var intermediaryDescription = RandomHelper.String();
        using (var dbContext = new MemCheckDbContext(db))
        {
            var request = UpdateCardHelper.RequestForFrontSideChange(card, RandomHelper.String(), versionCreator: otherUserId, versionDescription: intermediaryDescription);
            request = request with { AdditionalInfo = RandomHelper.String() };
            await new UpdateCard(dbContext.AsCallContext(), intermediaryDate).RunAsync(request);
        }

        var newestDate = RandomHelper.Date();
        var newestDescription = RandomHelper.String();
        using (var dbContext = new MemCheckDbContext(db))
        {
            var cardFromDb = await dbContext.Cards
                .Include(c => c.VersionCreator)
                .Include(c => c.VersionCreator)
                .Include(c => c.Images)
                .Include(c => c.CardLanguage)
                .Include(c => c.TagsInCards)
                .Include(c => c.UsersWithView)
                .SingleAsync(c => c.Id == card.Id);
            await new UpdateCard(dbContext.AsCallContext(), newestDate).RunAsync(UpdateCardHelper.RequestForBackSideChange(cardFromDb, RandomHelper.String(), versionDescription: newestDescription, versionCreator: userId));
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var versions = (await new GetCardVersions(dbContext.AsCallContext()).RunAsync(new GetCardVersions.Request(otherUserId, card.Id))).ToList();

            Assert.AreEqual((await dbContext.CardPreviousVersions.SingleAsync(c => c.VersionUtcDate == oldestDate)).Id, versions[2].VersionId);
            Assert.AreEqual(oldestDate, versions[2].VersionUtcDate);
            Assert.AreEqual(userName, versions[2].VersionCreator);
            Assert.AreEqual(oldestDescription, versions[2].VersionDescription);
            Assert.AreEqual(6, versions[2].ChangedFieldNames.Count());

            Assert.AreEqual((await dbContext.CardPreviousVersions.SingleAsync(c => c.VersionUtcDate == intermediaryDate)).Id, versions[1].VersionId);
            Assert.AreEqual(intermediaryDate, versions[1].VersionUtcDate);
            Assert.AreEqual(otherUserName, versions[1].VersionCreator);
            Assert.AreEqual(intermediaryDescription, versions[1].VersionDescription);
            Assert.AreEqual(2, versions[1].ChangedFieldNames.Count());
            Assert.IsTrue(versions[1].ChangedFieldNames.Any(f => f == "FrontSide"));
            Assert.IsTrue(versions[1].ChangedFieldNames.Any(f => f == "AdditionalInfo"));

            Assert.IsNull(versions[0].VersionId);
            Assert.AreEqual(newestDate, versions[0].VersionUtcDate);
            Assert.AreEqual(userName, versions[0].VersionCreator);
            Assert.AreEqual(newestDescription, versions[0].VersionDescription);
            Assert.AreEqual(1, versions[0].ChangedFieldNames.Count());
            Assert.AreEqual("BackSide", versions[0].ChangedFieldNames.First());
        }
    }
}
