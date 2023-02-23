using MemCheck.Application.Helpers;
using MemCheck.Application.QueryValidation;
using MemCheck.Basics;
using MemCheck.Database;
using MemCheck.Domain;
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
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new UpdateTag(dbContext.AsCallContext()).RunAsync(new UpdateTag.Request(userId, Guid.NewGuid(), RandomHelper.String(), "", RandomHelper.String())));
    }
    [TestMethod()]
    public async Task EmptyName()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var tag = await TagHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateTag(dbContext.AsCallContext()).RunAsync(new UpdateTag.Request(user.Id, tag, "", "", RandomHelper.String())));
    }
    [TestMethod()]
    public async Task NameNotTrimmed()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var tag = await TagHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new UpdateTag(dbContext.AsCallContext()).RunAsync(new UpdateTag.Request(user.Id, tag, RandomHelper.String(Tag.MinNameLength) + '\t', "", RandomHelper.String())));
    }
    [TestMethod()]
    public async Task DescriptionNotTrimmed()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var tag = await TagHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new UpdateTag(dbContext.AsCallContext()).RunAsync(new UpdateTag.Request(user.Id, tag, RandomHelper.String(), "\t", RandomHelper.String())));
    }
    [TestMethod()]
    public async Task NameTooShort()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var tag = await TagHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateTag(dbContext.AsCallContext()).RunAsync(new UpdateTag.Request(user.Id, tag, RandomHelper.String(Tag.MinNameLength - 1), "", RandomHelper.String())));
    }
    [TestMethod()]
    public async Task NameTooLong()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var tag = await TagHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateTag(dbContext.AsCallContext()).RunAsync(new UpdateTag.Request(user.Id, tag, RandomHelper.String(Tag.MaxNameLength + 1), "", RandomHelper.String())));
    }
    [TestMethod()]
    public async Task DescriptionTooLong()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var tag = await TagHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateTag(dbContext.AsCallContext()).RunAsync(new UpdateTag.Request(user.Id, tag, RandomHelper.String(), RandomHelper.String(Tag.MaxDescriptionLength + 1), RandomHelper.String())));
    }
    [TestMethod()]
    public async Task NameWithForbiddenChar()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var tag = await TagHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateTag(dbContext.AsCallContext()).RunAsync(new UpdateTag.Request(user.Id, tag, "a<b", "", RandomHelper.String())));
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
        await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateTag(dbContext.AsCallContext()).RunAsync(new UpdateTag.Request(user.Id, tag, name, "", RandomHelper.String())));
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
        await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateTag(dbContext.AsCallContext()).RunAsync(new UpdateTag.Request(user.Id, tag, name, description, RandomHelper.String())));
    }
    [TestMethod()]
    public async Task SuccessfulUpdateOfBothFields()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var tag = await TagHelper.CreateAsync(db, user);
        var name = RandomHelper.String();
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
            await Assert.ThrowsExceptionAsync<NonexistentUserException>(async () => await new UpdateTag(dbContext.AsCallContext()).RunAsync(new UpdateTag.Request(Guid.Empty, tag, RandomHelper.String(), RandomHelper.String(), RandomHelper.String())));

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
            await Assert.ThrowsExceptionAsync<NonexistentUserException>(async () => await new UpdateTag(dbContext.AsCallContext()).RunAsync(new UpdateTag.Request(Guid.NewGuid(), tag, RandomHelper.String(), RandomHelper.String(), RandomHelper.String())));

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
        var e = await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateTag(callContext).RunAsync(new UpdateTag.Request(user.Id, persoTagId, RandomHelper.String(), description, RandomHelper.String())));
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
        var e = await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateTag(callContext).RunAsync(new UpdateTag.Request(user.Id, persoTagId, Tag.Perso, RandomHelper.String(), RandomHelper.String())));
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
        var e = await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateTag(callContext).RunAsync(new UpdateTag.Request(user.Id, persoTagId, RandomHelper.String(), RandomHelper.String(), RandomHelper.String())));
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
        await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateTag(callContext).RunAsync(new UpdateTag.Request(user.Id, persoTagId, RandomHelper.String(), description, RandomHelper.String())));
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
}
