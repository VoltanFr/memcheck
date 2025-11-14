using MemCheck.Application.Helpers;
using MemCheck.Application.QueryValidation;
using MemCheck.Basics;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace MemCheck.Application.Tags;

[TestClass()]
public class UpdateTagTests
{
    [TestMethod()]
    public async Task DoesNotExist()
    {
        var db = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(db);
        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await new UpdateTag(dbContext.AsCallContext()).RunAsync(new UpdateTag.Request(userId, Guid.NewGuid(), RandomHelper.String(), "", RandomHelper.String())));
    }
    [TestMethod()]
    public async Task EmptyName()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var tag = await TagHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExactlyAsync<RequestInputException>(async () => await new UpdateTag(dbContext.AsCallContext()).RunAsync(new UpdateTag.Request(user.Id, tag, "", "", RandomHelper.String())));
    }
    [TestMethod()]
    public async Task NameNotTrimmed()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var tag = await TagHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await new UpdateTag(dbContext.AsCallContext()).RunAsync(new UpdateTag.Request(user.Id, tag, RandomHelper.String(Tag.MinNameLength) + '\t', "", RandomHelper.String())));
    }
    [TestMethod()]
    public async Task DescriptionNotTrimmed()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var tag = await TagHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await new UpdateTag(dbContext.AsCallContext()).RunAsync(new UpdateTag.Request(user.Id, tag, RandomHelper.String(), "\t", RandomHelper.String())));
    }
    [TestMethod()]
    public async Task NameTooShort()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var tag = await TagHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExactlyAsync<RequestInputException>(async () => await new UpdateTag(dbContext.AsCallContext()).RunAsync(new UpdateTag.Request(user.Id, tag, RandomHelper.String(Tag.MinNameLength - 1), "", RandomHelper.String())));
    }
    [TestMethod()]
    public async Task NameTooLong()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var tag = await TagHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExactlyAsync<RequestInputException>(async () => await new UpdateTag(dbContext.AsCallContext()).RunAsync(new UpdateTag.Request(user.Id, tag, RandomHelper.String(Tag.MaxNameLength + 1), "", RandomHelper.String())));
    }
    [TestMethod()]
    public async Task DescriptionTooLong()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var tag = await TagHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExactlyAsync<RequestInputException>(async () => await new UpdateTag(dbContext.AsCallContext()).RunAsync(new UpdateTag.Request(user.Id, tag, RandomHelper.String(), RandomHelper.String(Tag.MaxDescriptionLength + 1), RandomHelper.String())));
    }
    [TestMethod()]
    public async Task NameWithForbiddenChar()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var tag = await TagHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExactlyAsync<RequestInputException>(async () => await new UpdateTag(dbContext.AsCallContext()).RunAsync(new UpdateTag.Request(user.Id, tag, "a<b", "", RandomHelper.String())));
    }
    [TestMethod()]
    public async Task AlreadyExists()
    {
        var name = RandomHelper.String();
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var tag = await TagHelper.CreateAsync(db, user);
        var otherTag = await TagHelper.CreateAsync(db, user, name);
        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExactlyAsync<RequestInputException>(async () => await new UpdateTag(dbContext.AsCallContext()).RunAsync(new UpdateTag.Request(user.Id, tag, name, "", RandomHelper.String())));
    }
    [TestMethod()]
    public async Task NoChange()
    {
        var name = RandomHelper.String();
        var description = RandomHelper.String();
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var tag = await TagHelper.CreateAsync(db, user, name, description);
        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExactlyAsync<RequestInputException>(async () => await new UpdateTag(dbContext.AsCallContext()).RunAsync(new UpdateTag.Request(user.Id, tag, name, description, RandomHelper.String())));
    }
    [TestMethod()]
    public async Task SuccessfulUpdateOfAllFields()
    {
        var db = DbHelper.GetEmptyTestDB();
        var initialVersionCreatingUser = await UserHelper.CreateUserInDbAsync(db);
        var newVersionCreatingUser = await UserHelper.CreateUserInDbAsync(db);

        var initialTag = await TagHelper.CreateTagAsync(db, initialVersionCreatingUser.Id);

        var newName = RandomHelper.String();
        var newDescription = RandomHelper.String();
        var runDate = RandomHelper.Date();
        var newVersionDescription = RandomHelper.String();

        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateTag(dbContext.AsCallContext(), runDate).RunAsync(new UpdateTag.Request(newVersionCreatingUser.Id, initialTag.Id, newName, newDescription, newVersionDescription));

        using (var dbContext = new MemCheckDbContext(db))
        {
            var loadedTag = await dbContext.Tags
                .Include(tag => tag.CreatingUser)
                .Include(tag => tag.TagsInCards)
                .Include(tag => tag.PreviousVersion)
                .SingleAsync();

            Assert.AreEqual(newName, loadedTag.Name);
            Assert.AreEqual(newDescription, loadedTag.Description);
            Assert.AreEqual(newVersionCreatingUser.Id, loadedTag.CreatingUser.Id);
            Assert.AreEqual(runDate, loadedTag.VersionUtcDate);
            Assert.AreEqual(newVersionDescription, loadedTag.VersionDescription);
            Assert.AreEqual(TagVersionType.Changes, loadedTag.VersionType);
            Assert.IsNotNull(loadedTag.PreviousVersion);

            var previousVersionFromLoaded = await dbContext.TagPreviousVersions
                .Include(tag => tag.CreatingUser)
                .Include(tag => tag.PreviousVersion)
                .SingleAsync();

            Assert.AreEqual(loadedTag.Id, previousVersionFromLoaded.Tag);
            Assert.AreEqual(initialVersionCreatingUser.Id, previousVersionFromLoaded.CreatingUser.Id);
            Assert.AreEqual(initialTag.Name, previousVersionFromLoaded.Name);
            Assert.AreEqual(initialTag.Description, previousVersionFromLoaded.Description);
            Assert.AreEqual(initialTag.VersionDescription, previousVersionFromLoaded.VersionDescription);
            Assert.AreEqual(TagVersionType.Creation, previousVersionFromLoaded.VersionType);
            Assert.AreEqual(initialTag.VersionUtcDate, previousVersionFromLoaded.VersionUtcDate);
            Assert.IsNull(previousVersionFromLoaded.PreviousVersion);
        }
    }
    [TestMethod()]
    public async Task SuccessfulUpdateOfName()
    {
        var db = DbHelper.GetEmptyTestDB();
        var description = RandomHelper.String();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var tag = await TagHelper.CreateAsync(db, user, description: description);
        var name = RandomHelper.String();
        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateTag(dbContext.AsCallContext()).RunAsync(new UpdateTag.Request(user.Id, tag, name, description, RandomHelper.String()));
        using (var dbContext = new MemCheckDbContext(db))
        {
            var loaded = await new GetTag(dbContext.AsCallContext()).RunAsync(new GetTag.Request(tag));
            Assert.AreEqual(name, loaded.TagName);
            Assert.AreEqual(description, loaded.Description);
            Assert.AreEqual(user.UserName, loaded.CreatingUserName);
        }
    }
    [TestMethod()]
    public async Task SuccessfulUpdateOfDescription()
    {
        var db = DbHelper.GetEmptyTestDB();
        var name = RandomHelper.String();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var tag = await TagHelper.CreateAsync(db, user, name);
        var description = RandomHelper.String();
        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateTag(dbContext.AsCallContext()).RunAsync(new UpdateTag.Request(user.Id, tag, name, description, RandomHelper.String()));
        using (var dbContext = new MemCheckDbContext(db))
        {
            var loaded = await new GetTag(dbContext.AsCallContext()).RunAsync(new GetTag.Request(tag));
            Assert.AreEqual(name, loaded.TagName);
            Assert.AreEqual(description, loaded.Description);
            Assert.AreEqual(user.UserName, loaded.CreatingUserName);
        }
    }
    [TestMethod()]
    public async Task UserNotLoggedIn()
    {
        var db = DbHelper.GetEmptyTestDB();

        var user = await UserHelper.CreateUserInDbAsync(db);
        var name = RandomHelper.String();
        var tag = await TagHelper.CreateAsync(db, user, name);

        using (var dbContext = new MemCheckDbContext(db))
            await Assert.ThrowsExactlyAsync<NonexistentUserException>(async () => await new UpdateTag(dbContext.AsCallContext()).RunAsync(new UpdateTag.Request(Guid.Empty, tag, RandomHelper.String(), RandomHelper.String(), RandomHelper.String())));

        using (var dbContext = new MemCheckDbContext(db))
            Assert.AreEqual(name, (await new GetTag(dbContext.AsCallContext()).RunAsync(new GetTag.Request(tag))).TagName);
    }
    [TestMethod()]
    public async Task UnknownUser()
    {
        var db = DbHelper.GetEmptyTestDB();

        var user = await UserHelper.CreateUserInDbAsync(db);
        var name = RandomHelper.String();
        var tag = await TagHelper.CreateAsync(db, user, name);

        using (var dbContext = new MemCheckDbContext(db))
            await Assert.ThrowsExactlyAsync<NonexistentUserException>(async () => await new UpdateTag(dbContext.AsCallContext()).RunAsync(new UpdateTag.Request(Guid.NewGuid(), tag, RandomHelper.String(), RandomHelper.String(), RandomHelper.String())));

        using (var dbContext = new MemCheckDbContext(db))
            Assert.AreEqual(name, (await new GetTag(dbContext.AsCallContext()).RunAsync(new GetTag.Request(tag))).TagName);
    }
    [TestMethod()]
    public async Task NonAdminCanNotRenamePersoTag()
    {
        var db = DbHelper.GetEmptyTestDB();
        var description = RandomHelper.String();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var persoTagId = await TagHelper.CreateAsync(db, user, name: Tag.Perso, description: description);

        using var dbContext = new MemCheckDbContext(db);
        var errorMesg = RandomHelper.String();
        var localizer = new TestLocalizer("OnlyAdminsCanModifyPersoTag".PairedWith(errorMesg));
        var callContext = dbContext.AsCallContext(localizer);
        var e = await Assert.ThrowsExactlyAsync<RequestInputException>(async () => await new UpdateTag(callContext).RunAsync(new UpdateTag.Request(user.Id, persoTagId, RandomHelper.String(), description, RandomHelper.String())));
        Assert.AreEqual(errorMesg, e.Message);
    }
    [TestMethod()]
    public async Task NonAdminCanNotChangeDescriptionOfPersoTag()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var persoTagId = await TagHelper.CreateAsync(db, user, name: Tag.Perso, description: RandomHelper.String());

        using var dbContext = new MemCheckDbContext(db);
        var roleChecker = new TestRoleChecker();
        var errorMesg = RandomHelper.String();
        var localizer = new TestLocalizer("OnlyAdminsCanModifyPersoTag".PairedWith(errorMesg));
        var callContext = dbContext.AsCallContext(localizer, roleChecker);
        var e = await Assert.ThrowsExactlyAsync<RequestInputException>(async () => await new UpdateTag(callContext).RunAsync(new UpdateTag.Request(user.Id, persoTagId, Tag.Perso, RandomHelper.String(), RandomHelper.String())));
        Assert.AreEqual(errorMesg, e.Message);
    }
    [TestMethod()]
    public async Task NonAdminCanNotRenameAndChangeDescriptionOfPersoTag()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var persoTagId = await TagHelper.CreateAsync(db, user, name: Tag.Perso, description: RandomHelper.String());

        using var dbContext = new MemCheckDbContext(db);
        var errorMesg = RandomHelper.String();
        var localizer = new TestLocalizer("OnlyAdminsCanModifyPersoTag".PairedWith(errorMesg));
        var callContext = dbContext.AsCallContext(localizer);
        var e = await Assert.ThrowsExactlyAsync<RequestInputException>(async () => await new UpdateTag(callContext).RunAsync(new UpdateTag.Request(user.Id, persoTagId, RandomHelper.String(), RandomHelper.String(), RandomHelper.String())));
        Assert.AreEqual(errorMesg, e.Message);
    }
    [TestMethod()]
    public async Task AdminCanNotRenamePersoTag()
    {
        var db = DbHelper.GetEmptyTestDB();
        var description = RandomHelper.String();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var persoTagId = await TagHelper.CreateAsync(db, user, name: Tag.Perso, description: description);

        using var dbContext = new MemCheckDbContext(db);
        var roleChecker = new TestRoleChecker(user.Id);
        var errorMesg = RandomHelper.String();
        var localizer = new TestLocalizer("OnlyAdminsCanModifyPersoTag".PairedWith(errorMesg));
        var callContext = dbContext.AsCallContext(localizer, roleChecker);
        await Assert.ThrowsExactlyAsync<RequestInputException>(async () => await new UpdateTag(callContext).RunAsync(new UpdateTag.Request(user.Id, persoTagId, RandomHelper.String(), description, RandomHelper.String())));
    }
    [TestMethod()]
    public async Task AdminCanChangeDescriptionOfPersoTag()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var persoTagId = await TagHelper.CreateAsync(db, user, name: Tag.Perso, description: RandomHelper.String());

        var newDescription = RandomHelper.String();

        using (var dbContext = new MemCheckDbContext(db))
        {
            var roleChecker = new TestRoleChecker(user.Id);
            var callContext = dbContext.AsCallContext(roleChecker);
            await new UpdateTag(callContext).RunAsync(new UpdateTag.Request(user.Id, persoTagId, Tag.Perso, newDescription, RandomHelper.String()));
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var loaded = await new GetTag(dbContext.AsCallContext()).RunAsync(new GetTag.Request(persoTagId));
            Assert.AreEqual(Tag.Perso, loaded.TagName);
            Assert.AreEqual(newDescription, loaded.Description);
        }
    }
    [TestMethod()]
    public async Task ComplexCase()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user1 = await UserHelper.CreateUserInDbAsync(db);
        var user2 = await UserHelper.CreateUserInDbAsync(db);
        var user3 = await UserHelper.CreateUserInDbAsync(db);

        var initialTag1 = await TagHelper.CreateTagAsync(db, user1.Id);
        var initialTag2 = await TagHelper.CreateTagAsync(db, user2.Id);

        // user2 updates tag1's name
        var tag1NameInFirstUpdate = RandomHelper.String();
        var tag1FirstUpdateRunDate = RandomHelper.Date(initialTag1.VersionUtcDate);
        var tag1FirstUpdateVersionDescription = RandomHelper.String();
        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateTag(dbContext.AsCallContext(), tag1FirstUpdateRunDate).RunAsync(new UpdateTag.Request(user2.Id, initialTag1.Id, tag1NameInFirstUpdate, initialTag1.Description, tag1FirstUpdateVersionDescription));

        // user2 updates tag2's description
        var tag2NewDescription = RandomHelper.String();
        var tag2UpdateRunDate = RandomHelper.Date(initialTag1.VersionUtcDate);
        var tag2NewVersionUpdateDescription = RandomHelper.String();
        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateTag(dbContext.AsCallContext(), tag2UpdateRunDate).RunAsync(new UpdateTag.Request(user2.Id, initialTag2.Id, initialTag2.Name, tag2NewDescription, tag2NewVersionUpdateDescription));

        // user3 updates tag1's name and description
        var tag1NameInSecondUpdate = RandomHelper.String();
        var tag1DescriptionInSecondUpdate = RandomHelper.String();
        var tag1SecondUpdateRunDate = RandomHelper.Date(tag1FirstUpdateRunDate);
        var tag1SecondUpdateVersionDescription = RandomHelper.String();
        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateTag(dbContext.AsCallContext(), tag1SecondUpdateRunDate).RunAsync(new UpdateTag.Request(user3.Id, initialTag1.Id, tag1NameInSecondUpdate, tag1DescriptionInSecondUpdate, tag1SecondUpdateVersionDescription));

        using (var dbContext = new MemCheckDbContext(db))
        {
            var currentVersionOfTag1 = await dbContext.Tags
                .Include(tag => tag.CreatingUser)
                .Include(tag => tag.PreviousVersion)
                .SingleAsync(tag => tag.Id == initialTag1.Id);

            Assert.AreEqual(tag1NameInSecondUpdate, currentVersionOfTag1.Name);
            Assert.AreEqual(tag1DescriptionInSecondUpdate, currentVersionOfTag1.Description);
            Assert.AreEqual(user3.Id, currentVersionOfTag1.CreatingUser.Id);
            Assert.AreEqual(tag1SecondUpdateRunDate, currentVersionOfTag1.VersionUtcDate);
            Assert.AreEqual(tag1SecondUpdateVersionDescription, currentVersionOfTag1.VersionDescription);
            Assert.AreEqual(TagVersionType.Changes, currentVersionOfTag1.VersionType);
            Assert.IsNotNull(currentVersionOfTag1.PreviousVersion);

            var tag1AfterFirstUpdate = await dbContext.TagPreviousVersions
                .Include(tagPreviousVersion => tagPreviousVersion.CreatingUser)
                .Include(tagPreviousVersion => tagPreviousVersion.PreviousVersion)
                .SingleAsync(tagPreviousVersion => tagPreviousVersion.Id == currentVersionOfTag1.PreviousVersion.Id);

            Assert.AreEqual(currentVersionOfTag1.Id, tag1AfterFirstUpdate.Tag);
            Assert.AreEqual(user2.Id, tag1AfterFirstUpdate.CreatingUser.Id);
            Assert.AreEqual(tag1NameInFirstUpdate, tag1AfterFirstUpdate.Name);
            Assert.AreEqual(initialTag1.Description, tag1AfterFirstUpdate.Description);
            Assert.AreEqual(tag1FirstUpdateVersionDescription, tag1AfterFirstUpdate.VersionDescription);
            Assert.AreEqual(TagVersionType.Changes, tag1AfterFirstUpdate.VersionType);
            Assert.AreEqual(tag1FirstUpdateRunDate, tag1AfterFirstUpdate.VersionUtcDate);
            Assert.IsNotNull(tag1AfterFirstUpdate.PreviousVersion);

            var tag1OldestVersion = await dbContext.TagPreviousVersions
                .Include(tag => tag.CreatingUser)
                .Include(tag => tag.PreviousVersion)
                .SingleAsync(tagPreviousVersion => tagPreviousVersion.Id == tag1AfterFirstUpdate.PreviousVersion.Id);

            Assert.AreEqual(currentVersionOfTag1.Id, tag1OldestVersion.Tag);
            Assert.AreEqual(user1.Id, tag1OldestVersion.CreatingUser.Id);
            Assert.AreEqual(initialTag1.Name, tag1OldestVersion.Name);
            Assert.AreEqual(initialTag1.Description, tag1OldestVersion.Description);
            Assert.AreEqual(initialTag1.VersionDescription, tag1OldestVersion.VersionDescription);
            Assert.AreEqual(TagVersionType.Creation, tag1OldestVersion.VersionType);
            Assert.AreEqual(initialTag1.VersionUtcDate, tag1OldestVersion.VersionUtcDate);
            Assert.IsNull(tag1OldestVersion.PreviousVersion);

            var currentVersionOfTag2 = await dbContext.Tags
                .Include(tag => tag.CreatingUser)
                .Include(tag => tag.PreviousVersion)
                .SingleAsync(tag => tag.Id == initialTag2.Id);

            Assert.AreEqual(initialTag2.Name, currentVersionOfTag2.Name);
            Assert.AreEqual(tag2NewDescription, currentVersionOfTag2.Description);
            Assert.AreEqual(user2.Id, currentVersionOfTag2.CreatingUser.Id);
            Assert.AreEqual(tag2UpdateRunDate, currentVersionOfTag2.VersionUtcDate);
            Assert.AreEqual(tag2NewVersionUpdateDescription, currentVersionOfTag2.VersionDescription);
            Assert.AreEqual(TagVersionType.Changes, currentVersionOfTag2.VersionType);
            Assert.IsNotNull(currentVersionOfTag2.PreviousVersion);

            var tag2OldVersion = await dbContext.TagPreviousVersions
                .Include(tag => tag.CreatingUser)
                .Include(tag => tag.PreviousVersion)
                .SingleAsync(tagPreviousVersion => tagPreviousVersion.Id == currentVersionOfTag2.PreviousVersion.Id);

            Assert.AreEqual(currentVersionOfTag2.Id, tag2OldVersion.Tag);
            Assert.AreEqual(user2.Id, tag2OldVersion.CreatingUser.Id);
            Assert.AreEqual(initialTag2.Name, tag2OldVersion.Name);
            Assert.AreEqual(initialTag2.Description, tag2OldVersion.Description);
            Assert.AreEqual(initialTag2.VersionDescription, tag2OldVersion.VersionDescription);
            Assert.AreEqual(TagVersionType.Creation, tag2OldVersion.VersionType);
            Assert.AreEqual(initialTag2.VersionUtcDate, tag2OldVersion.VersionUtcDate);
            Assert.IsNull(tag2OldVersion.PreviousVersion);
        }
    }
    [TestMethod()]
    public async Task UpdateDoesNotBreakStatsInCards()
    {
        var db = DbHelper.GetEmptyTestDB();

        var user1Id = await UserHelper.CreateInDbAsync(db);
        var user2Id = await UserHelper.CreateInDbAsync(db);

        var tag = await TagHelper.CreateTagAsync(db, user1Id);

        var card1Id = await CardHelper.CreateIdAsync(db, user1Id, tagIds: tag.Id.AsArray());
        var card2Id = await CardHelper.CreateIdAsync(db, user1Id, tagIds: tag.Id.AsArray());

        var user1RatingOnCard1 = RandomHelper.Rating();
        var user2RatingOnCard1 = RandomHelper.Rating();
        var user2RatingOnCard2 = RandomHelper.Rating();
        var expectedAverageRating = (((user1RatingOnCard1 + user2RatingOnCard1) / 2.0) + user2RatingOnCard2) / 2.0;

        await RatingHelper.RecordForUserAsync(db, user1Id, card1Id, user1RatingOnCard1);
        await RatingHelper.RecordForUserAsync(db, user2Id, card1Id, user2RatingOnCard1);
        await RatingHelper.RecordForUserAsync(db, user2Id, card2Id, user2RatingOnCard2);

        using (var dbContext = new MemCheckDbContext(db))
            await new RefreshTagStats(dbContext.AsCallContext()).RunAsync(new RefreshTagStats.Request());

        using (var dbContext = new MemCheckDbContext(db))
        {
            var tag1FromDb = await dbContext.Tags.SingleAsync();
            Assert.AreEqual(expectedAverageRating, tag1FromDb.AverageRatingOfPublicCards);
            Assert.AreEqual(2, tag1FromDb.CountOfPublicCards);
        }

        var newTagName = RandomHelper.String();

        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateTag(dbContext.AsCallContext()).RunAsync(new UpdateTag.Request(user1Id, tag.Id, newTagName, tag.Description, RandomHelper.String()));

        using (var dbContext = new MemCheckDbContext(db))
        {
            var loadedTag = await dbContext.Tags
                .Include(tag => tag.CreatingUser)
                .Include(tag => tag.TagsInCards)
                .Include(tag => tag.PreviousVersion)
                .SingleAsync();

            Assert.AreEqual(newTagName, loadedTag.Name);
            Assert.AreEqual(expectedAverageRating, loadedTag.AverageRatingOfPublicCards);
            Assert.AreEqual(2, loadedTag.CountOfPublicCards);
        }
    }
    [TestMethod()]
    public async Task ThreeUpdates()
    {
        // Checks that the linked list of previous versions is correct

        var db = DbHelper.GetEmptyTestDB();
        var creatingUser = await UserHelper.CreateUserInDbAsync(db);
        var tagId = await TagHelper.CreateAsync(db, creatingUser);

        var firstUpdateUser = await UserHelper.CreateUserInDbAsync(db);
        var nameAfterFirstUpdate = RandomHelper.String();
        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateTag(dbContext.AsCallContext()).RunAsync(new UpdateTag.Request(firstUpdateUser.Id, tagId, nameAfterFirstUpdate, RandomHelper.String(), RandomHelper.String()));

        var secondUpdateUser = await UserHelper.CreateUserInDbAsync(db);
        var nameAfterSecondUpdate = RandomHelper.String();
        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateTag(dbContext.AsCallContext()).RunAsync(new UpdateTag.Request(secondUpdateUser.Id, tagId, nameAfterSecondUpdate, RandomHelper.String(), RandomHelper.String()));

        var thirdUpdateUser = await UserHelper.CreateUserInDbAsync(db);
        var nameAfterThirdUpdate = RandomHelper.String();
        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateTag(dbContext.AsCallContext()).RunAsync(new UpdateTag.Request(thirdUpdateUser.Id, tagId, nameAfterThirdUpdate, RandomHelper.String(), RandomHelper.String()));

        using (var dbContext = new MemCheckDbContext(db))
        {
            var currentTag = await dbContext.Tags
                .Include(tag => tag.CreatingUser)
                .Include(tag => tag.PreviousVersion)
                .SingleAsync();
            Assert.AreEqual(thirdUpdateUser.UserName, currentTag.CreatingUser.UserName);
            Assert.AreEqual(nameAfterThirdUpdate, currentTag.Name);

            var allPreviousVersions = await dbContext.TagPreviousVersions
                .AsNoTracking()
                .Include(tagPreviousVersion => tagPreviousVersion.CreatingUser)
                .Include(tagPreviousVersion => tagPreviousVersion.PreviousVersion)
                .ToImmutableDictionaryAsync(tagPreviousVersion => tagPreviousVersion.Id, tagPreviousVersion => tagPreviousVersion);

            var firstPreviousVersion = allPreviousVersions[currentTag.PreviousVersion!.Id];
            Assert.AreEqual(secondUpdateUser.UserName, firstPreviousVersion.CreatingUser.UserName);
            Assert.AreEqual(nameAfterSecondUpdate, firstPreviousVersion.Name);

            var secondPreviousVersion = allPreviousVersions[firstPreviousVersion.PreviousVersion!.Id];
            Assert.AreEqual(firstUpdateUser.UserName, secondPreviousVersion.CreatingUser.UserName);
            Assert.AreEqual(nameAfterFirstUpdate, secondPreviousVersion.Name);

            var thirdPreviousVersion = allPreviousVersions[secondPreviousVersion.PreviousVersion!.Id];
            Assert.AreEqual(creatingUser.UserName, thirdPreviousVersion.CreatingUser.UserName);
        }
    }
}
