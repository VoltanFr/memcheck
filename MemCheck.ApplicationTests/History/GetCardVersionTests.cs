﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using MemCheck.Database;
using System.Linq;
using MemCheck.Application.Tests.Helpers;
using MemCheck.Application.CardChanging;
using Microsoft.EntityFrameworkCore;

namespace MemCheck.Application.History
{
    [TestClass()]
    public class GetCardVersionTests
    {
        [TestMethod()]
        public async Task UserNotLoggedIn()
        {
            using (var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB()))
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetCardVersion(dbContext).RunAsync(new GetCardVersion.Request(Guid.Empty, Guid.Empty)));
        }
        [TestMethod()]
        public async Task UserDoesNotExist()
        {
            using (var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB()))
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetCardVersion(dbContext).RunAsync(new GetCardVersion.Request(Guid.NewGuid(), Guid.Empty)));
        }
        [TestMethod()]
        public async Task VersionDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(db);
            using (var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB()))
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetCardVersion(dbContext).RunAsync(new GetCardVersion.Request(userId, Guid.NewGuid())));
        }
        [TestMethod()]
        public async Task FailIfUserCanNotView()
        {
            var db = DbHelper.GetEmptyTestDB();
            var userId = await UserHelper.CreateInDbAsync(db);
            var language = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, userId, language: language, userWithViewIds: new[] { userId }); //Private
            using (var dbContext = new MemCheckDbContext(db))
                await new UpdateCard(dbContext).RunAsync(UpdateCardHelper.RequestForVisibilityChange(card, userWithViewIds: Array.Empty<Guid>()), new TestLocalizer());    //Now public
            var otherUserId = await UserHelper.CreateInDbAsync(db);
            using (var dbContext = new MemCheckDbContext(db))
            {
                var versionId = (await new GetCardVersions(dbContext, new TestLocalizer()).RunAsync(new GetCardVersions.Request(otherUserId, card.Id))).Single(v => v.VersionId != null).VersionId!.Value;

                var version = await new GetCardVersion(dbContext).RunAsync(new GetCardVersion.Request(userId, versionId));
                Assert.AreEqual(1, version.UsersWithVisibility.Count());
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetCardVersion(dbContext).RunAsync(new GetCardVersion.Request(otherUserId, versionId)));
            }
        }
        [TestMethod()]
        public async Task MultipleVersions()
        {
            var db = DbHelper.GetEmptyTestDB();
            var language = await CardLanguagHelper.CreateAsync(db);

            var initialVersionCreatorName = StringHelper.RandomString();
            var initialVersionCreatorId = await UserHelper.CreateInDbAsync(db, userName: initialVersionCreatorName);
            var initialVersionDate = DateHelper.Random();
            var initialVersionDescription = StringHelper.RandomString();
            var initialVersionFrontSide = StringHelper.RandomString();
            var card = await CardHelper.CreateAsync(db, initialVersionCreatorId, language: language, versionDate: initialVersionDate, versionDescription: initialVersionDescription, frontSide: initialVersionFrontSide);

            var intermediaryVersionCreatorName = StringHelper.RandomString();
            var intermediaryVersionCreatorId = await UserHelper.CreateInDbAsync(db, userName: intermediaryVersionCreatorName);
            var intermediaryVersionDate = DateHelper.Random();
            var intermediaryVersionDescription = StringHelper.RandomString();
            var intermediaryVersionFrontSide = StringHelper.RandomString();
            using (var dbContext = new MemCheckDbContext(db))
                await new UpdateCard(dbContext).RunAsync(UpdateCardHelper.RequestForFrontSideChange(card, intermediaryVersionFrontSide, versionCreator: intermediaryVersionCreatorId, versionDescription: intermediaryVersionDescription), new TestLocalizer(), intermediaryVersionDate);

            using (var dbContext = new MemCheckDbContext(db))
                //We need a new version to exist but don't mind about its contents
                await new UpdateCard(dbContext).RunAsync(UpdateCardHelper.RequestForBackSideChange(card, StringHelper.RandomString(), versionDescription: StringHelper.RandomString(), versionCreator: initialVersionCreatorId), new TestLocalizer(), DateHelper.Random());

            using (var dbContext = new MemCheckDbContext(db))
            {
                var versions = (await new GetCardVersions(dbContext, new TestLocalizer()).RunAsync(new GetCardVersions.Request(intermediaryVersionCreatorId, card.Id))).Where(v => v.VersionId != null).Select(v => v.VersionId!.Value).ToList();
                var initialVersionId = versions[1];
                var intermediaryVersionId = versions[0];

                var initialVersion = await new GetCardVersion(dbContext).RunAsync(new GetCardVersion.Request(initialVersionCreatorId, initialVersionId));
                Assert.AreEqual(initialVersionDescription, initialVersion.VersionDescription);
                Assert.AreEqual(initialVersionDate, initialVersion.VersionUtcDate);
                Assert.AreEqual(initialVersionCreatorName, initialVersion.CreatorName);
                Assert.AreEqual(initialVersionFrontSide, initialVersion.FrontSide);

                var intermediaryVersion = await new GetCardVersion(dbContext).RunAsync(new GetCardVersion.Request(initialVersionCreatorId, intermediaryVersionId));
                Assert.AreEqual(intermediaryVersionDescription, intermediaryVersion.VersionDescription);
                Assert.AreEqual(intermediaryVersionDate, intermediaryVersion.VersionUtcDate);
                Assert.AreEqual(intermediaryVersionCreatorName, intermediaryVersion.CreatorName);
                Assert.AreEqual(intermediaryVersionFrontSide, intermediaryVersion.FrontSide);
            }
        }
    }
}