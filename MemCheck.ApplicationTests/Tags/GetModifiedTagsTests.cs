using MemCheck.Application.Helpers;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Tags;

[TestClass()]
public class GetModifiedTagsTests
{
    [TestMethod()]
    public async Task None()
    {
        using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
        var getter = new GetModifiedTags(dbContext.AsCallContext());
        var request = new GetModifiedTags.Request(DateTime.MinValue);
        var result = await getter.RunAsync(request);
        Assert.AreEqual(0, result.Tags.Length);
    }
    [TestMethod()]
    public async Task OneTagWithoutUpdates_NotToBeReported()
    {
        var db = DbHelper.GetEmptyTestDB();

        var user = await UserHelper.CreateUserInDbAsync(db);
        var tagCreationDate = RandomHelper.Date();
        await TagHelper.CreateTagAsync(db, user.Id, versionUtcDate: tagCreationDate);

        using var dbContext = new MemCheckDbContext(db);
        var getter = new GetModifiedTags(dbContext.AsCallContext());
        var request = new GetModifiedTags.Request(tagCreationDate.AddMinutes(1));
        var result = await getter.RunAsync(request);
        Assert.AreEqual(0, result.Tags.Length);
    }
    [TestMethod()]
    public async Task OneTagWithoutUpdates_ToBeReported()
    {
        var db = DbHelper.GetEmptyTestDB();

        var user = await UserHelper.CreateUserInDbAsync(db);
        var tagCreationDate = RandomHelper.Date();
        var createdTag = await TagHelper.CreateTagAsync(db, user.Id, versionUtcDate: tagCreationDate);

        using var dbContext = new MemCheckDbContext(db);
        var getter = new GetModifiedTags(dbContext.AsCallContext());
        var request = new GetModifiedTags.Request(tagCreationDate);
        var result = await getter.RunAsync(request);
        Assert.AreEqual(1, result.Tags.Length);
        var resultTag = result.Tags.Single();
        Assert.AreEqual(createdTag.Id, resultTag.TagId);
        Assert.AreEqual(0, resultTag.CountOfPublicCards);
        Assert.AreEqual(0, resultTag.AverageRatingOfPublicCards);
        Assert.AreEqual(1, resultTag.Versions.Length);
        var resultTagVersion = resultTag.Versions.Single();
        Assert.AreEqual(user.UserName, resultTagVersion.CreatorName);
        Assert.AreEqual(createdTag.Name, resultTagVersion.TagName);
        Assert.AreEqual(createdTag.Description, resultTagVersion.Description);
        Assert.AreEqual(createdTag.VersionDescription, resultTagVersion.VersionDescription);
        Assert.AreEqual(createdTag.VersionUtcDate, resultTagVersion.UtcDate);
        Assert.AreEqual(TagVersionType.Creation, resultTagVersion.VersionType);
    }
    [TestMethod()]
    public async Task TwoTagsWithoutUpdates()
    {
        var db = DbHelper.GetEmptyTestDB();

        var user1 = await UserHelper.CreateUserInDbAsync(db);
        var tag1CreationDate = RandomHelper.Date();
        await TagHelper.CreateTagAsync(db, user1.Id, versionUtcDate: tag1CreationDate);

        var requestDate = RandomHelper.Date(tag1CreationDate);

        var user2 = await UserHelper.CreateUserInDbAsync(db);
        var tag2CreationDate = RandomHelper.Date(requestDate);
        var createdTag2 = await TagHelper.CreateTagAsync(db, user2.Id, versionUtcDate: tag2CreationDate);

        using var dbContext = new MemCheckDbContext(db);
        var getter = new GetModifiedTags(dbContext.AsCallContext());
        var request = new GetModifiedTags.Request(requestDate);
        var result = await getter.RunAsync(request);
        Assert.AreEqual(1, result.Tags.Length);
        var resultTag = result.Tags.Single();
        Assert.AreEqual(createdTag2.Id, resultTag.TagId);
        Assert.AreEqual(0, resultTag.CountOfPublicCards);
        Assert.AreEqual(0, resultTag.AverageRatingOfPublicCards);
        Assert.AreEqual(1, resultTag.Versions.Length);
        var resultTagVersion = resultTag.Versions.Single();
        Assert.AreEqual(user2.UserName, resultTagVersion.CreatorName);
        Assert.AreEqual(createdTag2.Name, resultTagVersion.TagName);
        Assert.AreEqual(createdTag2.Description, resultTagVersion.Description);
        Assert.AreEqual(createdTag2.VersionDescription, resultTagVersion.VersionDescription);
        Assert.AreEqual(createdTag2.VersionUtcDate, resultTagVersion.UtcDate);
    }
    [TestMethod()]
    public async Task OneTagWithOneUpdate_NothingToBeReported()
    {
        var db = DbHelper.GetEmptyTestDB();

        var user = await UserHelper.CreateUserInDbAsync(db);
        var tagCreationDate = RandomHelper.Date();
        var tag = await TagHelper.CreateTagAsync(db, user.Id, versionUtcDate: tagCreationDate);

        var tagUpdateDate = RandomHelper.Date(tagCreationDate);
        var newTagName = RandomHelper.String();
        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateTag(dbContext.AsCallContext(), tagUpdateDate).RunAsync(new UpdateTag.Request(user.Id, tag.Id, newTagName, tag.Description, RandomHelper.String()));

        using (var dbContext = new MemCheckDbContext(db))
        {
            var getter = new GetModifiedTags(dbContext.AsCallContext());
            var request = new GetModifiedTags.Request(RandomHelper.Date(tagUpdateDate));
            var result = await getter.RunAsync(request);
            Assert.AreEqual(0, result.Tags.Length);
        }
    }
    [TestMethod()]
    public async Task OneTagWithOneUpdate_OnlyCurrentVersionIsNew()
    {
        var db = DbHelper.GetEmptyTestDB();

        var user = await UserHelper.CreateUserInDbAsync(db);
        var tagCreationDate = RandomHelper.Date();
        var createdTag = await TagHelper.CreateTagAsync(db, user.Id, versionUtcDate: tagCreationDate);

        var sinceDate = RandomHelper.Date(tagCreationDate);
        var tagUpdateDate = RandomHelper.Date(sinceDate);

        var newTagName = RandomHelper.String();
        var newVersionDescription = RandomHelper.String();
        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateTag(dbContext.AsCallContext(), tagUpdateDate).RunAsync(new UpdateTag.Request(user.Id, createdTag.Id, newTagName, createdTag.Description, newVersionDescription));

        using (var dbContext = new MemCheckDbContext(db))
        {
            var getter = new GetModifiedTags(dbContext.AsCallContext());
            var request = new GetModifiedTags.Request(sinceDate);
            var result = await getter.RunAsync(request);
            Assert.AreEqual(1, result.Tags.Length);
            var resultTag = result.Tags.Single();
            Assert.AreEqual(createdTag.Id, resultTag.TagId);
            Assert.AreEqual(0, resultTag.CountOfPublicCards);
            Assert.AreEqual(0, resultTag.AverageRatingOfPublicCards);
            Assert.AreEqual(2, resultTag.Versions.Length);
            var resultTagNewestVersion = resultTag.Versions.First();
            Assert.AreEqual(user.UserName, resultTagNewestVersion.CreatorName);
            Assert.AreEqual(newTagName, resultTagNewestVersion.TagName);
            Assert.AreEqual(createdTag.Description, resultTagNewestVersion.Description);
            Assert.AreEqual(newVersionDescription, resultTagNewestVersion.VersionDescription);
            Assert.AreEqual(tagUpdateDate, resultTagNewestVersion.UtcDate);
            Assert.AreEqual(TagVersionType.Changes, resultTagNewestVersion.VersionType);
            var resultTagOldestVersion = resultTag.Versions.Last();
            Assert.AreEqual(user.UserName, resultTagOldestVersion.CreatorName);
            Assert.AreEqual(createdTag.Name, resultTagOldestVersion.TagName);
            Assert.AreEqual(createdTag.Description, resultTagOldestVersion.Description);
            Assert.AreEqual(createdTag.VersionDescription, resultTagOldestVersion.VersionDescription);
            Assert.AreEqual(tagCreationDate, resultTagOldestVersion.UtcDate);
            Assert.AreEqual(TagVersionType.Creation, resultTagOldestVersion.VersionType);
        }
    }
    [TestMethod()]
    public async Task OneTagWithOneUpdate_BothAreNew()
    {
        var db = DbHelper.GetEmptyTestDB();

        var user = await UserHelper.CreateUserInDbAsync(db);
        var sinceDate = RandomHelper.Date();
        var tagCreationDate = RandomHelper.Date(sinceDate);
        var createdTag = await TagHelper.CreateTagAsync(db, user.Id, versionUtcDate: tagCreationDate);

        var tagUpdateDate = RandomHelper.Date(tagCreationDate);
        var newTagName = RandomHelper.String();
        var newVersionDescription = RandomHelper.String();
        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateTag(dbContext.AsCallContext(), tagUpdateDate).RunAsync(new UpdateTag.Request(user.Id, createdTag.Id, newTagName, createdTag.Description, newVersionDescription));

        using (var dbContext = new MemCheckDbContext(db))
        {
            var getter = new GetModifiedTags(dbContext.AsCallContext());
            var request = new GetModifiedTags.Request(sinceDate);
            var result = await getter.RunAsync(request);
            Assert.AreEqual(1, result.Tags.Length);
            var resultTag = result.Tags.Single();
            Assert.AreEqual(createdTag.Id, resultTag.TagId);
            Assert.AreEqual(0, resultTag.CountOfPublicCards);
            Assert.AreEqual(0, resultTag.AverageRatingOfPublicCards);
            Assert.AreEqual(2, resultTag.Versions.Length);
            var resultTagNewestVersion = resultTag.Versions.First();
            Assert.AreEqual(user.UserName, resultTagNewestVersion.CreatorName);
            Assert.AreEqual(newTagName, resultTagNewestVersion.TagName);
            Assert.AreEqual(createdTag.Description, resultTagNewestVersion.Description);
            Assert.AreEqual(newVersionDescription, resultTagNewestVersion.VersionDescription);
            Assert.AreEqual(tagUpdateDate, resultTagNewestVersion.UtcDate);
            Assert.AreEqual(TagVersionType.Changes, resultTagNewestVersion.VersionType);
            var resultTagOldestVersion = resultTag.Versions.Last();
            Assert.AreEqual(user.UserName, resultTagOldestVersion.CreatorName);
            Assert.AreEqual(createdTag.Name, resultTagOldestVersion.TagName);
            Assert.AreEqual(createdTag.Description, resultTagOldestVersion.Description);
            Assert.AreEqual(createdTag.VersionDescription, resultTagOldestVersion.VersionDescription);
            Assert.AreEqual(tagCreationDate, resultTagOldestVersion.UtcDate);
            Assert.AreEqual(TagVersionType.Creation, resultTagOldestVersion.VersionType);
        }
    }
    [TestMethod()]
    public async Task OneTagWithTwoUpdates_NothingToBeReported()
    {
        var db = DbHelper.GetEmptyTestDB();

        var user = await UserHelper.CreateUserInDbAsync(db);
        var tagCreationDate = RandomHelper.Date();
        var tag = await TagHelper.CreateTagAsync(db, user.Id, versionUtcDate: tagCreationDate);

        var tagFirstUpdateDate = RandomHelper.Date(tagCreationDate);
        var tagNameAfterFirstUpdate = RandomHelper.String();
        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateTag(dbContext.AsCallContext(), tagFirstUpdateDate).RunAsync(new UpdateTag.Request(user.Id, tag.Id, tagNameAfterFirstUpdate, tag.Description, RandomHelper.String()));

        var tagSecondUpdateDate = RandomHelper.Date(tagFirstUpdateDate);
        var tagDescriptionAfterSecondUpdate = RandomHelper.String();
        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateTag(dbContext.AsCallContext(), tagSecondUpdateDate).RunAsync(new UpdateTag.Request(user.Id, tag.Id, tagNameAfterFirstUpdate, tagDescriptionAfterSecondUpdate, RandomHelper.String()));

        using (var dbContext = new MemCheckDbContext(db))
        {
            var getter = new GetModifiedTags(dbContext.AsCallContext());
            var request = new GetModifiedTags.Request(RandomHelper.Date(tagSecondUpdateDate));
            var result = await getter.RunAsync(request);
            Assert.AreEqual(0, result.Tags.Length);
        }
    }
    [TestMethod()]
    public async Task OneTagWithTwoUpdates_OnlyCurrentVersionIsNew()
    {
        var db = DbHelper.GetEmptyTestDB();

        var user = await UserHelper.CreateUserInDbAsync(db);
        var tagCreationDate = RandomHelper.Date();
        var createdTag = await TagHelper.CreateTagAsync(db, user.Id, versionUtcDate: tagCreationDate);

        var tagFirstUpdateDate = RandomHelper.Date(tagCreationDate);
        var nameAfterFirstUpdate = RandomHelper.String();
        var descriptionAfterFirstUpdate = RandomHelper.String();
        var firstUpdateVersionDescription = RandomHelper.String();
        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateTag(dbContext.AsCallContext(), tagFirstUpdateDate).RunAsync(new UpdateTag.Request(user.Id, createdTag.Id, nameAfterFirstUpdate, descriptionAfterFirstUpdate, firstUpdateVersionDescription));

        var sinceDate = RandomHelper.Date(tagFirstUpdateDate);

        var tagSecondUpdateDate = RandomHelper.Date(sinceDate);
        var nameAfterSecondUpdate = RandomHelper.String();
        var descriptionAfterSecondUpdate = RandomHelper.String();
        var secondUpdateVersionDescription = RandomHelper.String();
        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateTag(dbContext.AsCallContext(), tagSecondUpdateDate).RunAsync(new UpdateTag.Request(user.Id, createdTag.Id, nameAfterSecondUpdate, descriptionAfterSecondUpdate, secondUpdateVersionDescription));

        using (var dbContext = new MemCheckDbContext(db))
        {
            var getter = new GetModifiedTags(dbContext.AsCallContext());
            var request = new GetModifiedTags.Request(sinceDate);
            var result = await getter.RunAsync(request);
            Assert.AreEqual(1, result.Tags.Length);
            var resultTag = result.Tags.Single();
            Assert.AreEqual(createdTag.Id, resultTag.TagId);
            Assert.AreEqual(0, resultTag.CountOfPublicCards);
            Assert.AreEqual(0, resultTag.AverageRatingOfPublicCards);
            Assert.AreEqual(2, resultTag.Versions.Length);
            var resultTagNewestVersion = resultTag.Versions.First();
            Assert.AreEqual(user.UserName, resultTagNewestVersion.CreatorName);
            Assert.AreEqual(nameAfterSecondUpdate, resultTagNewestVersion.TagName);
            Assert.AreEqual(descriptionAfterSecondUpdate, resultTagNewestVersion.Description);
            Assert.AreEqual(secondUpdateVersionDescription, resultTagNewestVersion.VersionDescription);
            Assert.AreEqual(tagSecondUpdateDate, resultTagNewestVersion.UtcDate);
            Assert.AreEqual(TagVersionType.Changes, resultTagNewestVersion.VersionType);
            var resultTagOldestVersion = resultTag.Versions.Last();
            Assert.AreEqual(user.UserName, resultTagOldestVersion.CreatorName);
            Assert.AreEqual(nameAfterFirstUpdate, resultTagOldestVersion.TagName);
            Assert.AreEqual(descriptionAfterFirstUpdate, resultTagOldestVersion.Description);
            Assert.AreEqual(firstUpdateVersionDescription, resultTagOldestVersion.VersionDescription);
            Assert.AreEqual(tagFirstUpdateDate, resultTagOldestVersion.UtcDate);
            Assert.AreEqual(TagVersionType.Changes, resultTagOldestVersion.VersionType);
        }
    }
    [TestMethod()]
    public async Task OneTagWithTwoUpdates_TwoVersionsAreNew()
    {
        var db = DbHelper.GetEmptyTestDB();

        var user = await UserHelper.CreateUserInDbAsync(db);
        var tagCreationDate = RandomHelper.Date();
        var createdTag = await TagHelper.CreateTagAsync(db, user.Id, versionUtcDate: tagCreationDate);

        var sinceDate = RandomHelper.Date(tagCreationDate);

        var tagFirstUpdateDate = RandomHelper.Date(sinceDate);
        var nameAfterFirstUpdate = RandomHelper.String();
        var descriptionAfterFirstUpdate = RandomHelper.String();
        var firstUpdateVersionDescription = RandomHelper.String();
        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateTag(dbContext.AsCallContext(), tagFirstUpdateDate).RunAsync(new UpdateTag.Request(user.Id, createdTag.Id, nameAfterFirstUpdate, descriptionAfterFirstUpdate, firstUpdateVersionDescription));

        var tagSecondUpdateDate = RandomHelper.Date(tagFirstUpdateDate);
        var nameAfterSecondUpdate = RandomHelper.String();
        var descriptionAfterSecondUpdate = RandomHelper.String();
        var secondUpdateVersionDescription = RandomHelper.String();
        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateTag(dbContext.AsCallContext(), tagSecondUpdateDate).RunAsync(new UpdateTag.Request(user.Id, createdTag.Id, nameAfterSecondUpdate, descriptionAfterSecondUpdate, secondUpdateVersionDescription));

        using (var dbContext = new MemCheckDbContext(db))
        {
            var getter = new GetModifiedTags(dbContext.AsCallContext());
            var request = new GetModifiedTags.Request(sinceDate);
            var result = await getter.RunAsync(request);
            Assert.AreEqual(1, result.Tags.Length);
            var resultTag = result.Tags.Single();
            Assert.AreEqual(createdTag.Id, resultTag.TagId);
            Assert.AreEqual(0, resultTag.CountOfPublicCards);
            Assert.AreEqual(0, resultTag.AverageRatingOfPublicCards);
            Assert.AreEqual(3, resultTag.Versions.Length);
            {
                var resultTagNewestVersion = resultTag.Versions[0];
                Assert.AreEqual(user.UserName, resultTagNewestVersion.CreatorName);
                Assert.AreEqual(nameAfterSecondUpdate, resultTagNewestVersion.TagName);
                Assert.AreEqual(descriptionAfterSecondUpdate, resultTagNewestVersion.Description);
                Assert.AreEqual(secondUpdateVersionDescription, resultTagNewestVersion.VersionDescription);
                Assert.AreEqual(tagSecondUpdateDate, resultTagNewestVersion.UtcDate);
                Assert.AreEqual(TagVersionType.Changes, resultTagNewestVersion.VersionType);
            }
            {
                var resultTagOldestVersion = resultTag.Versions[1];
                Assert.AreEqual(user.UserName, resultTagOldestVersion.CreatorName);
                Assert.AreEqual(nameAfterFirstUpdate, resultTagOldestVersion.TagName);
                Assert.AreEqual(descriptionAfterFirstUpdate, resultTagOldestVersion.Description);
                Assert.AreEqual(firstUpdateVersionDescription, resultTagOldestVersion.VersionDescription);
                Assert.AreEqual(tagFirstUpdateDate, resultTagOldestVersion.UtcDate);
                Assert.AreEqual(TagVersionType.Changes, resultTagOldestVersion.VersionType);
            }
            {
                var resultTagOldestVersion = resultTag.Versions[2];
                Assert.AreEqual(user.UserName, resultTagOldestVersion.CreatorName);
                Assert.AreEqual(createdTag.Name, resultTagOldestVersion.TagName);
                Assert.AreEqual(createdTag.Description, resultTagOldestVersion.Description);
                Assert.AreEqual(createdTag.VersionDescription, resultTagOldestVersion.VersionDescription);
                Assert.AreEqual(createdTag.VersionUtcDate, resultTagOldestVersion.UtcDate);
                Assert.AreEqual(TagVersionType.Creation, resultTagOldestVersion.VersionType);
            }
        }
    }
    [TestMethod()]
    public async Task OneTagWithTwoUpdates_AllVersionsAreNew()
    {
        var db = DbHelper.GetEmptyTestDB();

        var sinceDate = RandomHelper.Date();

        var user = await UserHelper.CreateUserInDbAsync(db);
        var tagCreationDate = RandomHelper.Date(sinceDate);
        var createdTag = await TagHelper.CreateTagAsync(db, user.Id, versionUtcDate: tagCreationDate);

        var tagFirstUpdateDate = RandomHelper.Date(tagCreationDate);
        var nameAfterFirstUpdate = RandomHelper.String();
        var descriptionAfterFirstUpdate = RandomHelper.String();
        var firstUpdateVersionDescription = RandomHelper.String();
        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateTag(dbContext.AsCallContext(), tagFirstUpdateDate).RunAsync(new UpdateTag.Request(user.Id, createdTag.Id, nameAfterFirstUpdate, descriptionAfterFirstUpdate, firstUpdateVersionDescription));

        var tagSecondUpdateDate = RandomHelper.Date(tagFirstUpdateDate);
        var nameAfterSecondUpdate = RandomHelper.String();
        var descriptionAfterSecondUpdate = RandomHelper.String();
        var secondUpdateVersionDescription = RandomHelper.String();
        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateTag(dbContext.AsCallContext(), tagSecondUpdateDate).RunAsync(new UpdateTag.Request(user.Id, createdTag.Id, nameAfterSecondUpdate, descriptionAfterSecondUpdate, secondUpdateVersionDescription));

        using (var dbContext = new MemCheckDbContext(db))
        {
            var getter = new GetModifiedTags(dbContext.AsCallContext());
            var request = new GetModifiedTags.Request(sinceDate);
            var result = await getter.RunAsync(request);
            Assert.AreEqual(1, result.Tags.Length);
            var resultTag = result.Tags.Single();
            Assert.AreEqual(createdTag.Id, resultTag.TagId);
            Assert.AreEqual(0, resultTag.CountOfPublicCards);
            Assert.AreEqual(0, resultTag.AverageRatingOfPublicCards);
            Assert.AreEqual(3, resultTag.Versions.Length);
            {
                var resultTagNewestVersion = resultTag.Versions[0];
                Assert.AreEqual(user.UserName, resultTagNewestVersion.CreatorName);
                Assert.AreEqual(nameAfterSecondUpdate, resultTagNewestVersion.TagName);
                Assert.AreEqual(descriptionAfterSecondUpdate, resultTagNewestVersion.Description);
                Assert.AreEqual(secondUpdateVersionDescription, resultTagNewestVersion.VersionDescription);
                Assert.AreEqual(tagSecondUpdateDate, resultTagNewestVersion.UtcDate);
                Assert.AreEqual(TagVersionType.Changes, resultTagNewestVersion.VersionType);
            }
            {
                var resultTagOldestVersion = resultTag.Versions[1];
                Assert.AreEqual(user.UserName, resultTagOldestVersion.CreatorName);
                Assert.AreEqual(nameAfterFirstUpdate, resultTagOldestVersion.TagName);
                Assert.AreEqual(descriptionAfterFirstUpdate, resultTagOldestVersion.Description);
                Assert.AreEqual(firstUpdateVersionDescription, resultTagOldestVersion.VersionDescription);
                Assert.AreEqual(tagFirstUpdateDate, resultTagOldestVersion.UtcDate);
                Assert.AreEqual(TagVersionType.Changes, resultTagOldestVersion.VersionType);
            }
            {
                var resultTagOldestVersion = resultTag.Versions[2];
                Assert.AreEqual(user.UserName, resultTagOldestVersion.CreatorName);
                Assert.AreEqual(createdTag.Name, resultTagOldestVersion.TagName);
                Assert.AreEqual(createdTag.Description, resultTagOldestVersion.Description);
                Assert.AreEqual(createdTag.VersionDescription, resultTagOldestVersion.VersionDescription);
                Assert.AreEqual(createdTag.VersionUtcDate, resultTagOldestVersion.UtcDate);
                Assert.AreEqual(TagVersionType.Creation, resultTagOldestVersion.VersionType);
            }
        }
    }
    [TestMethod()]
    public async Task ComplexCase()
    {
        var db = DbHelper.GetEmptyTestDB();

        var user1 = await UserHelper.CreateUserInDbAsync(db);
        var user2 = await UserHelper.CreateUserInDbAsync(db);
        var user3 = await UserHelper.CreateUserInDbAsync(db);

        var sinceDate1 = RandomHelper.Date();

        var tag1CreationDate = RandomHelper.Date(sinceDate1);
        var createdTag1 = await TagHelper.CreateTagAsync(db, user1.Id, versionUtcDate: tag1CreationDate);

        var sinceDate2 = RandomHelper.Date(tag1CreationDate);

        var tag1FirstUpdateDate = RandomHelper.Date(sinceDate2);
        var tag1NameAfterFirstUpdate = RandomHelper.String();
        var tag1DescriptionAfterFirstUpdate = RandomHelper.String();
        var tag1FirstUpdateVersionDescription = RandomHelper.String();
        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateTag(dbContext.AsCallContext(), tag1FirstUpdateDate).RunAsync(new UpdateTag.Request(user2.Id, createdTag1.Id, tag1NameAfterFirstUpdate, tag1DescriptionAfterFirstUpdate, tag1FirstUpdateVersionDescription));

        var sinceDate3 = RandomHelper.Date(tag1FirstUpdateDate);

        var tag2CreationDate = RandomHelper.Date(sinceDate3);
        var createdTag2 = await TagHelper.CreateTagAsync(db, user3.Id, versionUtcDate: tag2CreationDate);

        var sinceDate4 = RandomHelper.Date(tag2CreationDate);

        var tag1SecondUpdateDate = RandomHelper.Date(sinceDate4);
        var tag1NameAfterSecondUpdate = RandomHelper.String();
        var tag1DescriptionAfterSecondUpdate = RandomHelper.String();
        var tag1SecondUpdateVersionDescription = RandomHelper.String();
        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateTag(dbContext.AsCallContext(), tag1SecondUpdateDate).RunAsync(new UpdateTag.Request(user1.Id, createdTag1.Id, tag1NameAfterSecondUpdate, tag1DescriptionAfterSecondUpdate, tag1SecondUpdateVersionDescription));

        var sinceDate5 = RandomHelper.Date(tag1SecondUpdateDate);

        var tag1ThirdUpdateDate = RandomHelper.Date(sinceDate5);
        var tag1NameAfterThirdUpdate = RandomHelper.String();
        var tag1DescriptionAfterThirdUpdate = RandomHelper.String();
        var tag1ThirdUpdateVersionDescription = RandomHelper.String();
        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateTag(dbContext.AsCallContext(), tag1ThirdUpdateDate).RunAsync(new UpdateTag.Request(user2.Id, createdTag1.Id, tag1NameAfterThirdUpdate, tag1DescriptionAfterThirdUpdate, tag1ThirdUpdateVersionDescription));

        var sinceDate6 = RandomHelper.Date(tag1ThirdUpdateDate);

        var tag2FirstUpdateDate = RandomHelper.Date(sinceDate6);
        var tag2NameAfterFirstUpdate = RandomHelper.String();
        var tag2DescriptionAfterFirstUpdate = RandomHelper.String();
        var tag2FirstUpdateVersionDescription = RandomHelper.String();
        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateTag(dbContext.AsCallContext(), tag2FirstUpdateDate).RunAsync(new UpdateTag.Request(user3.Id, createdTag2.Id, tag2NameAfterFirstUpdate, tag2DescriptionAfterFirstUpdate, tag2FirstUpdateVersionDescription));

        var sinceDate7 = RandomHelper.Date(tag2FirstUpdateDate);

        var tag1FourthUpdateDate = RandomHelper.Date(sinceDate7);
        var tag1NameAfterFourthUpdate = RandomHelper.String();
        var tag1DescriptionAfterFourthUpdate = RandomHelper.String();
        var tag1FourthUpdateVersionDescription = RandomHelper.String();
        using (var dbContext = new MemCheckDbContext(db))
            await new UpdateTag(dbContext.AsCallContext(), tag1FourthUpdateDate).RunAsync(new UpdateTag.Request(user3.Id, createdTag1.Id, tag1NameAfterFourthUpdate, tag1DescriptionAfterFourthUpdate, tag1FourthUpdateVersionDescription));

        var sinceDate8 = RandomHelper.Date(tag1FourthUpdateDate);

        #region Checks on sinceDate8
        using (var dbContext = new MemCheckDbContext(db))
        {
            var getter = new GetModifiedTags(dbContext.AsCallContext());
            var request = new GetModifiedTags.Request(sinceDate8);
            var result = await getter.RunAsync(request);
            Assert.AreEqual(0, result.Tags.Length);
        }
        #endregion

        #region Checks on sinceDate7
        using (var dbContext = new MemCheckDbContext(db))
        {
            var getter = new GetModifiedTags(dbContext.AsCallContext());
            var request = new GetModifiedTags.Request(sinceDate7);
            var result = await getter.RunAsync(request);
            Assert.AreEqual(1, result.Tags.Length);
            var resultTag = result.Tags.Single();
            Assert.AreEqual(createdTag1.Id, resultTag.TagId);
            Assert.AreEqual(2, resultTag.Versions.Length);

            Assert.AreEqual(user3.UserName, resultTag.Versions[0].CreatorName);
            Assert.AreEqual(tag1NameAfterFourthUpdate, resultTag.Versions[0].TagName);
            Assert.AreEqual(tag1DescriptionAfterFourthUpdate, resultTag.Versions[0].Description);
            Assert.AreEqual(tag1FourthUpdateVersionDescription, resultTag.Versions[0].VersionDescription);
            Assert.AreEqual(tag1FourthUpdateDate, resultTag.Versions[0].UtcDate);
            Assert.AreEqual(TagVersionType.Changes, resultTag.Versions[0].VersionType);

            Assert.AreEqual(user2.UserName, resultTag.Versions[1].CreatorName);
            Assert.AreEqual(tag1NameAfterThirdUpdate, resultTag.Versions[1].TagName);
            Assert.AreEqual(tag1DescriptionAfterThirdUpdate, resultTag.Versions[1].Description);
            Assert.AreEqual(tag1ThirdUpdateVersionDescription, resultTag.Versions[1].VersionDescription);
            Assert.AreEqual(tag1ThirdUpdateDate, resultTag.Versions[1].UtcDate);
            Assert.AreEqual(TagVersionType.Changes, resultTag.Versions[1].VersionType);
        }
        #endregion

        #region Checks on sinceDate6
        using (var dbContext = new MemCheckDbContext(db))
        {
            var getter = new GetModifiedTags(dbContext.AsCallContext());
            var request = new GetModifiedTags.Request(sinceDate6);
            var result = await getter.RunAsync(request);
            Assert.AreEqual(2, result.Tags.Length);
            {
                var resultTag1 = result.Tags.Single(tag => tag.TagId == createdTag1.Id);
                Assert.AreEqual(2, resultTag1.Versions.Length);

                Assert.AreEqual(user3.UserName, resultTag1.Versions[0].CreatorName);
                Assert.AreEqual(tag1NameAfterFourthUpdate, resultTag1.Versions[0].TagName);
                Assert.AreEqual(tag1DescriptionAfterFourthUpdate, resultTag1.Versions[0].Description);
                Assert.AreEqual(tag1FourthUpdateVersionDescription, resultTag1.Versions[0].VersionDescription);
                Assert.AreEqual(tag1FourthUpdateDate, resultTag1.Versions[0].UtcDate);
                Assert.AreEqual(TagVersionType.Changes, resultTag1.Versions[0].VersionType);

                Assert.AreEqual(user2.UserName, resultTag1.Versions[1].CreatorName);
                Assert.AreEqual(tag1NameAfterThirdUpdate, resultTag1.Versions[1].TagName);
                Assert.AreEqual(tag1DescriptionAfterThirdUpdate, resultTag1.Versions[1].Description);
                Assert.AreEqual(tag1ThirdUpdateVersionDescription, resultTag1.Versions[1].VersionDescription);
                Assert.AreEqual(tag1ThirdUpdateDate, resultTag1.Versions[1].UtcDate);
                Assert.AreEqual(TagVersionType.Changes, resultTag1.Versions[1].VersionType);
            }
            {
                var resultTag2 = result.Tags.Single(tag => tag.TagId == createdTag2.Id);
                Assert.AreEqual(2, resultTag2.Versions.Length);

                Assert.AreEqual(user3.UserName, resultTag2.Versions[0].CreatorName);
                Assert.AreEqual(tag2NameAfterFirstUpdate, resultTag2.Versions[0].TagName);
                Assert.AreEqual(tag2DescriptionAfterFirstUpdate, resultTag2.Versions[0].Description);
                Assert.AreEqual(tag2FirstUpdateVersionDescription, resultTag2.Versions[0].VersionDescription);
                Assert.AreEqual(tag2FirstUpdateDate, resultTag2.Versions[0].UtcDate);
                Assert.AreEqual(TagVersionType.Changes, resultTag2.Versions[0].VersionType);

                Assert.AreEqual(user3.UserName, resultTag2.Versions[1].CreatorName);
                Assert.AreEqual(createdTag2.Name, resultTag2.Versions[1].TagName);
                Assert.AreEqual(createdTag2.Description, resultTag2.Versions[1].Description);
                Assert.AreEqual(createdTag2.VersionDescription, resultTag2.Versions[1].VersionDescription);
                Assert.AreEqual(createdTag2.VersionUtcDate, resultTag2.Versions[1].UtcDate);
                Assert.AreEqual(TagVersionType.Creation, resultTag2.Versions[1].VersionType);
            }
        }
        #endregion

        #region Checks on sinceDate5
        using (var dbContext = new MemCheckDbContext(db))
        {
            var getter = new GetModifiedTags(dbContext.AsCallContext());
            var request = new GetModifiedTags.Request(sinceDate5);
            var result = await getter.RunAsync(request);
            Assert.AreEqual(2, result.Tags.Length);
            {
                var resultTag1 = result.Tags.Single(tag => tag.TagId == createdTag1.Id);
                Assert.AreEqual(3, resultTag1.Versions.Length);

                Assert.AreEqual(user3.UserName, resultTag1.Versions[0].CreatorName);
                Assert.AreEqual(tag1NameAfterFourthUpdate, resultTag1.Versions[0].TagName);
                Assert.AreEqual(tag1DescriptionAfterFourthUpdate, resultTag1.Versions[0].Description);
                Assert.AreEqual(tag1FourthUpdateVersionDescription, resultTag1.Versions[0].VersionDescription);
                Assert.AreEqual(tag1FourthUpdateDate, resultTag1.Versions[0].UtcDate);
                Assert.AreEqual(TagVersionType.Changes, resultTag1.Versions[0].VersionType);

                Assert.AreEqual(user2.UserName, resultTag1.Versions[1].CreatorName);
                Assert.AreEqual(tag1NameAfterThirdUpdate, resultTag1.Versions[1].TagName);
                Assert.AreEqual(tag1DescriptionAfterThirdUpdate, resultTag1.Versions[1].Description);
                Assert.AreEqual(tag1ThirdUpdateVersionDescription, resultTag1.Versions[1].VersionDescription);
                Assert.AreEqual(tag1ThirdUpdateDate, resultTag1.Versions[1].UtcDate);
                Assert.AreEqual(TagVersionType.Changes, resultTag1.Versions[1].VersionType);

                Assert.AreEqual(user1.UserName, resultTag1.Versions[2].CreatorName);
                Assert.AreEqual(tag1NameAfterSecondUpdate, resultTag1.Versions[2].TagName);
                Assert.AreEqual(tag1DescriptionAfterSecondUpdate, resultTag1.Versions[2].Description);
                Assert.AreEqual(tag1SecondUpdateVersionDescription, resultTag1.Versions[2].VersionDescription);
                Assert.AreEqual(tag1SecondUpdateDate, resultTag1.Versions[2].UtcDate);
                Assert.AreEqual(TagVersionType.Changes, resultTag1.Versions[2].VersionType);
            }
            {
                var resultTag2 = result.Tags.Single(tag => tag.TagId == createdTag2.Id);
                Assert.AreEqual(2, resultTag2.Versions.Length);

                Assert.AreEqual(user3.UserName, resultTag2.Versions[0].CreatorName);
                Assert.AreEqual(tag2NameAfterFirstUpdate, resultTag2.Versions[0].TagName);
                Assert.AreEqual(tag2DescriptionAfterFirstUpdate, resultTag2.Versions[0].Description);
                Assert.AreEqual(tag2FirstUpdateVersionDescription, resultTag2.Versions[0].VersionDescription);
                Assert.AreEqual(tag2FirstUpdateDate, resultTag2.Versions[0].UtcDate);
                Assert.AreEqual(TagVersionType.Changes, resultTag2.Versions[0].VersionType);

                Assert.AreEqual(user3.UserName, resultTag2.Versions[1].CreatorName);
                Assert.AreEqual(createdTag2.Name, resultTag2.Versions[1].TagName);
                Assert.AreEqual(createdTag2.Description, resultTag2.Versions[1].Description);
                Assert.AreEqual(createdTag2.VersionDescription, resultTag2.Versions[1].VersionDescription);
                Assert.AreEqual(createdTag2.VersionUtcDate, resultTag2.Versions[1].UtcDate);
                Assert.AreEqual(TagVersionType.Creation, resultTag2.Versions[1].VersionType);
            }
        }
        #endregion

        #region Checks on sinceDate4
        using (var dbContext = new MemCheckDbContext(db))
        {
            var getter = new GetModifiedTags(dbContext.AsCallContext());
            var request = new GetModifiedTags.Request(sinceDate4);
            var result = await getter.RunAsync(request);
            Assert.AreEqual(2, result.Tags.Length);
            {
                var resultTag1 = result.Tags.Single(tag => tag.TagId == createdTag1.Id);
                Assert.AreEqual(4, resultTag1.Versions.Length);

                Assert.AreEqual(user3.UserName, resultTag1.Versions[0].CreatorName);
                Assert.AreEqual(tag1NameAfterFourthUpdate, resultTag1.Versions[0].TagName);
                Assert.AreEqual(tag1DescriptionAfterFourthUpdate, resultTag1.Versions[0].Description);
                Assert.AreEqual(tag1FourthUpdateVersionDescription, resultTag1.Versions[0].VersionDescription);
                Assert.AreEqual(tag1FourthUpdateDate, resultTag1.Versions[0].UtcDate);
                Assert.AreEqual(TagVersionType.Changes, resultTag1.Versions[0].VersionType);

                Assert.AreEqual(user2.UserName, resultTag1.Versions[1].CreatorName);
                Assert.AreEqual(tag1NameAfterThirdUpdate, resultTag1.Versions[1].TagName);
                Assert.AreEqual(tag1DescriptionAfterThirdUpdate, resultTag1.Versions[1].Description);
                Assert.AreEqual(tag1ThirdUpdateVersionDescription, resultTag1.Versions[1].VersionDescription);
                Assert.AreEqual(tag1ThirdUpdateDate, resultTag1.Versions[1].UtcDate);
                Assert.AreEqual(TagVersionType.Changes, resultTag1.Versions[1].VersionType);

                Assert.AreEqual(user1.UserName, resultTag1.Versions[2].CreatorName);
                Assert.AreEqual(tag1NameAfterSecondUpdate, resultTag1.Versions[2].TagName);
                Assert.AreEqual(tag1DescriptionAfterSecondUpdate, resultTag1.Versions[2].Description);
                Assert.AreEqual(tag1SecondUpdateVersionDescription, resultTag1.Versions[2].VersionDescription);
                Assert.AreEqual(tag1SecondUpdateDate, resultTag1.Versions[2].UtcDate);
                Assert.AreEqual(TagVersionType.Changes, resultTag1.Versions[2].VersionType);

                Assert.AreEqual(user2.UserName, resultTag1.Versions[3].CreatorName);
                Assert.AreEqual(tag1NameAfterFirstUpdate, resultTag1.Versions[3].TagName);
                Assert.AreEqual(tag1DescriptionAfterFirstUpdate, resultTag1.Versions[3].Description);
                Assert.AreEqual(tag1FirstUpdateVersionDescription, resultTag1.Versions[3].VersionDescription);
                Assert.AreEqual(tag1FirstUpdateDate, resultTag1.Versions[3].UtcDate);
                Assert.AreEqual(TagVersionType.Changes, resultTag1.Versions[3].VersionType);
            }
            {
                var resultTag2 = result.Tags.Single(tag => tag.TagId == createdTag2.Id);
                Assert.AreEqual(2, resultTag2.Versions.Length);

                Assert.AreEqual(user3.UserName, resultTag2.Versions[0].CreatorName);
                Assert.AreEqual(tag2NameAfterFirstUpdate, resultTag2.Versions[0].TagName);
                Assert.AreEqual(tag2DescriptionAfterFirstUpdate, resultTag2.Versions[0].Description);
                Assert.AreEqual(tag2FirstUpdateVersionDescription, resultTag2.Versions[0].VersionDescription);
                Assert.AreEqual(tag2FirstUpdateDate, resultTag2.Versions[0].UtcDate);
                Assert.AreEqual(TagVersionType.Changes, resultTag2.Versions[0].VersionType);

                Assert.AreEqual(user3.UserName, resultTag2.Versions[1].CreatorName);
                Assert.AreEqual(createdTag2.Name, resultTag2.Versions[1].TagName);
                Assert.AreEqual(createdTag2.Description, resultTag2.Versions[1].Description);
                Assert.AreEqual(createdTag2.VersionDescription, resultTag2.Versions[1].VersionDescription);
                Assert.AreEqual(createdTag2.VersionUtcDate, resultTag2.Versions[1].UtcDate);
                Assert.AreEqual(TagVersionType.Creation, resultTag2.Versions[1].VersionType);
            }
        }
        #endregion

        #region Checks on sinceDate3
        using (var dbContext = new MemCheckDbContext(db))
        {
            var getter = new GetModifiedTags(dbContext.AsCallContext());
            var request = new GetModifiedTags.Request(sinceDate3);
            var result = await getter.RunAsync(request);
            Assert.AreEqual(2, result.Tags.Length);
            {
                var resultTag1 = result.Tags.Single(tag => tag.TagId == createdTag1.Id);
                Assert.AreEqual(4, resultTag1.Versions.Length);

                Assert.AreEqual(user3.UserName, resultTag1.Versions[0].CreatorName);
                Assert.AreEqual(tag1NameAfterFourthUpdate, resultTag1.Versions[0].TagName);
                Assert.AreEqual(tag1DescriptionAfterFourthUpdate, resultTag1.Versions[0].Description);
                Assert.AreEqual(tag1FourthUpdateVersionDescription, resultTag1.Versions[0].VersionDescription);
                Assert.AreEqual(tag1FourthUpdateDate, resultTag1.Versions[0].UtcDate);
                Assert.AreEqual(TagVersionType.Changes, resultTag1.Versions[0].VersionType);

                Assert.AreEqual(user2.UserName, resultTag1.Versions[1].CreatorName);
                Assert.AreEqual(tag1NameAfterThirdUpdate, resultTag1.Versions[1].TagName);
                Assert.AreEqual(tag1DescriptionAfterThirdUpdate, resultTag1.Versions[1].Description);
                Assert.AreEqual(tag1ThirdUpdateVersionDescription, resultTag1.Versions[1].VersionDescription);
                Assert.AreEqual(tag1ThirdUpdateDate, resultTag1.Versions[1].UtcDate);
                Assert.AreEqual(TagVersionType.Changes, resultTag1.Versions[1].VersionType);

                Assert.AreEqual(user1.UserName, resultTag1.Versions[2].CreatorName);
                Assert.AreEqual(tag1NameAfterSecondUpdate, resultTag1.Versions[2].TagName);
                Assert.AreEqual(tag1DescriptionAfterSecondUpdate, resultTag1.Versions[2].Description);
                Assert.AreEqual(tag1SecondUpdateVersionDescription, resultTag1.Versions[2].VersionDescription);
                Assert.AreEqual(tag1SecondUpdateDate, resultTag1.Versions[2].UtcDate);
                Assert.AreEqual(TagVersionType.Changes, resultTag1.Versions[2].VersionType);

                Assert.AreEqual(user2.UserName, resultTag1.Versions[3].CreatorName);
                Assert.AreEqual(tag1NameAfterFirstUpdate, resultTag1.Versions[3].TagName);
                Assert.AreEqual(tag1DescriptionAfterFirstUpdate, resultTag1.Versions[3].Description);
                Assert.AreEqual(tag1FirstUpdateVersionDescription, resultTag1.Versions[3].VersionDescription);
                Assert.AreEqual(tag1FirstUpdateDate, resultTag1.Versions[3].UtcDate);
                Assert.AreEqual(TagVersionType.Changes, resultTag1.Versions[3].VersionType);
            }
            {
                var resultTag2 = result.Tags.Single(tag => tag.TagId == createdTag2.Id);
                Assert.AreEqual(2, resultTag2.Versions.Length);

                Assert.AreEqual(user3.UserName, resultTag2.Versions[0].CreatorName);
                Assert.AreEqual(tag2NameAfterFirstUpdate, resultTag2.Versions[0].TagName);
                Assert.AreEqual(tag2DescriptionAfterFirstUpdate, resultTag2.Versions[0].Description);
                Assert.AreEqual(tag2FirstUpdateVersionDescription, resultTag2.Versions[0].VersionDescription);
                Assert.AreEqual(tag2FirstUpdateDate, resultTag2.Versions[0].UtcDate);
                Assert.AreEqual(TagVersionType.Changes, resultTag2.Versions[0].VersionType);

                Assert.AreEqual(user3.UserName, resultTag2.Versions[1].CreatorName);
                Assert.AreEqual(createdTag2.Name, resultTag2.Versions[1].TagName);
                Assert.AreEqual(createdTag2.Description, resultTag2.Versions[1].Description);
                Assert.AreEqual(createdTag2.VersionDescription, resultTag2.Versions[1].VersionDescription);
                Assert.AreEqual(createdTag2.VersionUtcDate, resultTag2.Versions[1].UtcDate);
                Assert.AreEqual(TagVersionType.Creation, resultTag2.Versions[1].VersionType);
            }
        }
        #endregion

        #region Checks on sinceDate2
        using (var dbContext = new MemCheckDbContext(db))
        {
            var getter = new GetModifiedTags(dbContext.AsCallContext());
            var request = new GetModifiedTags.Request(sinceDate2);
            var result = await getter.RunAsync(request);
            Assert.AreEqual(2, result.Tags.Length);
            {
                var resultTag1 = result.Tags.Single(tag => tag.TagId == createdTag1.Id);
                Assert.AreEqual(5, resultTag1.Versions.Length);

                Assert.AreEqual(user3.UserName, resultTag1.Versions[0].CreatorName);
                Assert.AreEqual(tag1NameAfterFourthUpdate, resultTag1.Versions[0].TagName);
                Assert.AreEqual(tag1DescriptionAfterFourthUpdate, resultTag1.Versions[0].Description);
                Assert.AreEqual(tag1FourthUpdateVersionDescription, resultTag1.Versions[0].VersionDescription);
                Assert.AreEqual(tag1FourthUpdateDate, resultTag1.Versions[0].UtcDate);
                Assert.AreEqual(TagVersionType.Changes, resultTag1.Versions[0].VersionType);

                Assert.AreEqual(user2.UserName, resultTag1.Versions[1].CreatorName);
                Assert.AreEqual(tag1NameAfterThirdUpdate, resultTag1.Versions[1].TagName);
                Assert.AreEqual(tag1DescriptionAfterThirdUpdate, resultTag1.Versions[1].Description);
                Assert.AreEqual(tag1ThirdUpdateVersionDescription, resultTag1.Versions[1].VersionDescription);
                Assert.AreEqual(tag1ThirdUpdateDate, resultTag1.Versions[1].UtcDate);
                Assert.AreEqual(TagVersionType.Changes, resultTag1.Versions[1].VersionType);

                Assert.AreEqual(user1.UserName, resultTag1.Versions[2].CreatorName);
                Assert.AreEqual(tag1NameAfterSecondUpdate, resultTag1.Versions[2].TagName);
                Assert.AreEqual(tag1DescriptionAfterSecondUpdate, resultTag1.Versions[2].Description);
                Assert.AreEqual(tag1SecondUpdateVersionDescription, resultTag1.Versions[2].VersionDescription);
                Assert.AreEqual(tag1SecondUpdateDate, resultTag1.Versions[2].UtcDate);
                Assert.AreEqual(TagVersionType.Changes, resultTag1.Versions[2].VersionType);

                Assert.AreEqual(user2.UserName, resultTag1.Versions[3].CreatorName);
                Assert.AreEqual(tag1NameAfterFirstUpdate, resultTag1.Versions[3].TagName);
                Assert.AreEqual(tag1DescriptionAfterFirstUpdate, resultTag1.Versions[3].Description);
                Assert.AreEqual(tag1FirstUpdateVersionDescription, resultTag1.Versions[3].VersionDescription);
                Assert.AreEqual(tag1FirstUpdateDate, resultTag1.Versions[3].UtcDate);
                Assert.AreEqual(TagVersionType.Changes, resultTag1.Versions[3].VersionType);

                Assert.AreEqual(user1.UserName, resultTag1.Versions[4].CreatorName);
                Assert.AreEqual(createdTag1.Name, resultTag1.Versions[4].TagName);
                Assert.AreEqual(createdTag1.Description, resultTag1.Versions[4].Description);
                Assert.AreEqual(createdTag1.VersionDescription, resultTag1.Versions[4].VersionDescription);
                Assert.AreEqual(createdTag1.VersionUtcDate, resultTag1.Versions[4].UtcDate);
                Assert.AreEqual(TagVersionType.Creation, resultTag1.Versions[4].VersionType);
            }
            {
                var resultTag2 = result.Tags.Single(tag => tag.TagId == createdTag2.Id);
                Assert.AreEqual(2, resultTag2.Versions.Length);

                Assert.AreEqual(user3.UserName, resultTag2.Versions[0].CreatorName);
                Assert.AreEqual(tag2NameAfterFirstUpdate, resultTag2.Versions[0].TagName);
                Assert.AreEqual(tag2DescriptionAfterFirstUpdate, resultTag2.Versions[0].Description);
                Assert.AreEqual(tag2FirstUpdateVersionDescription, resultTag2.Versions[0].VersionDescription);
                Assert.AreEqual(tag2FirstUpdateDate, resultTag2.Versions[0].UtcDate);
                Assert.AreEqual(TagVersionType.Changes, resultTag2.Versions[0].VersionType);

                Assert.AreEqual(user3.UserName, resultTag2.Versions[1].CreatorName);
                Assert.AreEqual(createdTag2.Name, resultTag2.Versions[1].TagName);
                Assert.AreEqual(createdTag2.Description, resultTag2.Versions[1].Description);
                Assert.AreEqual(createdTag2.VersionDescription, resultTag2.Versions[1].VersionDescription);
                Assert.AreEqual(createdTag2.VersionUtcDate, resultTag2.Versions[1].UtcDate);
                Assert.AreEqual(TagVersionType.Creation, resultTag2.Versions[1].VersionType);
            }
        }
        #endregion

        #region Checks on sinceDate1
        using (var dbContext = new MemCheckDbContext(db))
        {
            var getter = new GetModifiedTags(dbContext.AsCallContext());
            var request = new GetModifiedTags.Request(sinceDate1);
            var result = await getter.RunAsync(request);
            Assert.AreEqual(2, result.Tags.Length);
            {
                var resultTag1 = result.Tags.Single(tag => tag.TagId == createdTag1.Id);
                Assert.AreEqual(5, resultTag1.Versions.Length);

                Assert.AreEqual(user3.UserName, resultTag1.Versions[0].CreatorName);
                Assert.AreEqual(tag1NameAfterFourthUpdate, resultTag1.Versions[0].TagName);
                Assert.AreEqual(tag1DescriptionAfterFourthUpdate, resultTag1.Versions[0].Description);
                Assert.AreEqual(tag1FourthUpdateVersionDescription, resultTag1.Versions[0].VersionDescription);
                Assert.AreEqual(tag1FourthUpdateDate, resultTag1.Versions[0].UtcDate);
                Assert.AreEqual(TagVersionType.Changes, resultTag1.Versions[0].VersionType);

                Assert.AreEqual(user2.UserName, resultTag1.Versions[1].CreatorName);
                Assert.AreEqual(tag1NameAfterThirdUpdate, resultTag1.Versions[1].TagName);
                Assert.AreEqual(tag1DescriptionAfterThirdUpdate, resultTag1.Versions[1].Description);
                Assert.AreEqual(tag1ThirdUpdateVersionDescription, resultTag1.Versions[1].VersionDescription);
                Assert.AreEqual(tag1ThirdUpdateDate, resultTag1.Versions[1].UtcDate);
                Assert.AreEqual(TagVersionType.Changes, resultTag1.Versions[1].VersionType);

                Assert.AreEqual(user1.UserName, resultTag1.Versions[2].CreatorName);
                Assert.AreEqual(tag1NameAfterSecondUpdate, resultTag1.Versions[2].TagName);
                Assert.AreEqual(tag1DescriptionAfterSecondUpdate, resultTag1.Versions[2].Description);
                Assert.AreEqual(tag1SecondUpdateVersionDescription, resultTag1.Versions[2].VersionDescription);
                Assert.AreEqual(tag1SecondUpdateDate, resultTag1.Versions[2].UtcDate);
                Assert.AreEqual(TagVersionType.Changes, resultTag1.Versions[2].VersionType);

                Assert.AreEqual(user2.UserName, resultTag1.Versions[3].CreatorName);
                Assert.AreEqual(tag1NameAfterFirstUpdate, resultTag1.Versions[3].TagName);
                Assert.AreEqual(tag1DescriptionAfterFirstUpdate, resultTag1.Versions[3].Description);
                Assert.AreEqual(tag1FirstUpdateVersionDescription, resultTag1.Versions[3].VersionDescription);
                Assert.AreEqual(tag1FirstUpdateDate, resultTag1.Versions[3].UtcDate);
                Assert.AreEqual(TagVersionType.Changes, resultTag1.Versions[3].VersionType);

                Assert.AreEqual(user1.UserName, resultTag1.Versions[4].CreatorName);
                Assert.AreEqual(createdTag1.Name, resultTag1.Versions[4].TagName);
                Assert.AreEqual(createdTag1.Description, resultTag1.Versions[4].Description);
                Assert.AreEqual(createdTag1.VersionDescription, resultTag1.Versions[4].VersionDescription);
                Assert.AreEqual(createdTag1.VersionUtcDate, resultTag1.Versions[4].UtcDate);
                Assert.AreEqual(TagVersionType.Creation, resultTag1.Versions[4].VersionType);
            }
            {
                var resultTag2 = result.Tags.Single(tag => tag.TagId == createdTag2.Id);
                Assert.AreEqual(2, resultTag2.Versions.Length);

                Assert.AreEqual(user3.UserName, resultTag2.Versions[0].CreatorName);
                Assert.AreEqual(tag2NameAfterFirstUpdate, resultTag2.Versions[0].TagName);
                Assert.AreEqual(tag2DescriptionAfterFirstUpdate, resultTag2.Versions[0].Description);
                Assert.AreEqual(tag2FirstUpdateVersionDescription, resultTag2.Versions[0].VersionDescription);
                Assert.AreEqual(tag2FirstUpdateDate, resultTag2.Versions[0].UtcDate);
                Assert.AreEqual(TagVersionType.Changes, resultTag2.Versions[0].VersionType);

                Assert.AreEqual(user3.UserName, resultTag2.Versions[1].CreatorName);
                Assert.AreEqual(createdTag2.Name, resultTag2.Versions[1].TagName);
                Assert.AreEqual(createdTag2.Description, resultTag2.Versions[1].Description);
                Assert.AreEqual(createdTag2.VersionDescription, resultTag2.Versions[1].VersionDescription);
                Assert.AreEqual(createdTag2.VersionUtcDate, resultTag2.Versions[1].UtcDate);
                Assert.AreEqual(TagVersionType.Creation, resultTag2.Versions[1].VersionType);
            }
        }
        #endregion
    }
}
