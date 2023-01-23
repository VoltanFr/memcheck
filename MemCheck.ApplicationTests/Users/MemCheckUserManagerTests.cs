using MemCheck.Application.Decks;
using MemCheck.Application.Heaping;
using MemCheck.Application.Helpers;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Users;

[TestClass()]
public class MemCheckUserManagerTests
{
    #region Private
    #endregion

    [TestMethod()]
    public async Task NameTooLong()
    {
        var db = DbHelper.GetEmptyTestDB();

        using (var dbContext = new MemCheckDbContext(db))
        {
            using var userManager = UserHelper.GetUserManager(dbContext);

            var user = UserHelper.Create(userName: RandomHelper.String(MemCheckUserValidator.MaxUserNameLength + 1, true));
            var identityResult = await userManager.CreateAsync(user);
            Assert.IsFalse(identityResult.Succeeded);
            Assert.AreEqual(MemCheckUserValidator.UserNameBadLengthErrorCode, identityResult.Errors.Single().Code);
        }

        using (var dbContext = new MemCheckDbContext(db))
            Assert.IsFalse(await dbContext.Users.AnyAsync());
    }
    [TestMethod()]
    public async Task NameTooShort()
    {
        var db = DbHelper.GetEmptyTestDB();

        using (var dbContext = new MemCheckDbContext(db))
        {
            using var userManager = UserHelper.GetUserManager(dbContext);

            var user = UserHelper.Create(userName: RandomHelper.String(MemCheckUserValidator.MinUserNameLength - 1, true));
            var identityResult = await userManager.CreateAsync(user);
            Assert.IsFalse(identityResult.Succeeded);
            Assert.AreEqual(MemCheckUserValidator.UserNameBadLengthErrorCode, identityResult.Errors.Single().Code);
        }

        using (var dbContext = new MemCheckDbContext(db))
            Assert.IsFalse(await dbContext.Users.AnyAsync());
    }
    [DataTestMethod, DataRow("   "), DataRow(" \t "), DataRow(" \r  "), DataRow("\t\r\n"), DataRow("\t   "), DataRow(" aaa"), DataRow("aaa "), DataRow("\taaa"), DataRow("aaa\t"), DataRow("\naaa"), DataRow("aaa\n")]
    public async Task NameNotTrimmed(string userName)
    {
        var db = DbHelper.GetEmptyTestDB();

        using (var dbContext = new MemCheckDbContext(db))
        {
            using var userManager = UserHelper.GetUserManager(dbContext);

            var user = UserHelper.Create(userName: userName);
            var identityResult = await userManager.CreateAsync(user);
            Assert.IsFalse(identityResult.Succeeded);
            Assert.AreEqual(MemCheckUserValidator.UserNameNotTrimmedErrorCode, identityResult.Errors.Single().Code);
        }

        using (var dbContext = new MemCheckDbContext(db))
            Assert.IsFalse(await dbContext.Users.AnyAsync());
    }
    [DataTestMethod, DataRow("!Toto"), DataRow("@Me"), DataRow("5Guys"), DataRow("$Money")]
    public async Task NameDoesNotStartWithALetter(string userName)
    {
        var db = DbHelper.GetEmptyTestDB();

        using (var dbContext = new MemCheckDbContext(db))
        {
            using var userManager = UserHelper.GetUserManager(dbContext);

            var user = UserHelper.Create(userName: userName);
            var identityResult = await userManager.CreateAsync(user);
            Assert.IsFalse(identityResult.Succeeded);
            Assert.AreEqual(MemCheckUserValidator.UserNameNameDoesNotStartWithALetter, identityResult.Errors.Single().Code);
        }

        using (var dbContext = new MemCheckDbContext(db))
            Assert.IsFalse(await dbContext.Users.AnyAsync());
    }
    [TestMethod()]
    public async Task NameAlreadyUsed()
    {
        var originalUser = UserHelper.Create();
        var db = DbHelper.GetEmptyTestDB();

        using (var dbContext = new MemCheckDbContext(db))
        {
            using var userManager = UserHelper.GetUserManager(dbContext);
            var identityResult = await userManager.CreateAsync(originalUser);
            Assert.IsTrue(identityResult.Succeeded);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            using var userManager = UserHelper.GetUserManager(dbContext);
            var user = UserHelper.Create(userName: originalUser.UserName);
            var identityResult = await userManager.CreateAsync(user);
            Assert.IsFalse(identityResult.Succeeded);
            Assert.AreEqual(nameof(IdentityErrorDescriber.DuplicateUserName), identityResult.Errors.Single().Code);
        }

        using (var dbContext = new MemCheckDbContext(db))
            Assert.AreEqual(1, await dbContext.Users.CountAsync());
    }
    [DataTestMethod, DataRow(" "), DataRow(" \t "), DataRow(" \r  "), DataRow("\t\r\n"), DataRow("\t   "), DataRow(" gggg@ggg"), DataRow("gggg@ggg ")]
    public async Task EmailNotTrimmed(string email)
    {
        var db = DbHelper.GetEmptyTestDB();

        using (var dbContext = new MemCheckDbContext(db))
        {
            using var userManager = UserHelper.GetUserManager(dbContext);
            var user = UserHelper.Create(email: email);
            var identityResult = await userManager.CreateAsync(user);
            Assert.IsFalse(identityResult.Succeeded);
            Assert.IsTrue(identityResult.Errors.Any(error => error.Code == MemCheckUserValidator.EmailNotTrimmedErrorCode));
        }

        using (var dbContext = new MemCheckDbContext(db))
            Assert.IsFalse(await dbContext.Users.AnyAsync());
    }
    [DataTestMethod, DataRow("Toto\0!"), DataRow("A\u0002AHHKHJ"), DataRow("A\nAHHKHJ"), DataRow("AA\rHHKHJ")]
    public async Task ControlChar(string userName)
    {
        var db = DbHelper.GetEmptyTestDB();

        using (var dbContext = new MemCheckDbContext(db))
        {
            using var userManager = UserHelper.GetUserManager(dbContext);
            var user = UserHelper.Create(userName: userName);
            var identityResult = await userManager.CreateAsync(user);
            Assert.IsFalse(identityResult.Succeeded);
            Assert.IsTrue(identityResult.Errors.Any(error => error.Code == MemCheckUserValidator.UserNameContainsControlChar));
        }

        using (var dbContext = new MemCheckDbContext(db))
            Assert.IsFalse(await dbContext.Users.AnyAsync());
    }
    [DataTestMethod, DataRow("Toto\t$$$")]
    public async Task ForbiddenChar(string userName)
    {
        var db = DbHelper.GetEmptyTestDB();

        using (var dbContext = new MemCheckDbContext(db))
        {
            using var userManager = UserHelper.GetUserManager(dbContext);
            var user = UserHelper.Create(userName: userName);
            var identityResult = await userManager.CreateAsync(user);
            Assert.IsFalse(identityResult.Succeeded);
            Assert.IsTrue(identityResult.Errors.Any(error => error.Code == MemCheckUserValidator.UserNameContainsForbiddenChar));
        }

        using (var dbContext = new MemCheckDbContext(db))
            Assert.IsFalse(await dbContext.Users.AnyAsync());
    }
    [DataTestMethod, DataRow(""), DataRow("gggg"), DataRow("@x"), DataRow("@x.com"), DataRow("dsq@")]
    public async Task InvalidEmail(string email)
    {
        var db = DbHelper.GetEmptyTestDB();

        using (var dbContext = new MemCheckDbContext(db))
        {
            using var userManager = UserHelper.GetUserManager(dbContext);
            var user = UserHelper.Create(email: email);
            var identityResult = await userManager.CreateAsync(user);
            Assert.IsFalse(identityResult.Succeeded);
            Assert.AreEqual(nameof(IdentityErrorDescriber.InvalidEmail), identityResult.Errors.Single().Code);
        }

        using (var dbContext = new MemCheckDbContext(db))
            Assert.IsFalse(await dbContext.Users.AnyAsync());
    }
    [TestMethod()]
    public async Task DeleteMustFail()
    {
        var db = DbHelper.GetEmptyTestDB();
        var createdUser = await UserHelper.CreateUserInDbAsync(db);

        using (var dbContext = new MemCheckDbContext(db))
        {
            var userFromDb = await dbContext.Users.SingleAsync(u => u.Id == createdUser.Id);
            using var userManager = UserHelper.GetUserManager(dbContext);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await userManager.DeleteAsync(userFromDb));
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var user = await dbContext.Users.SingleAsync();
            Assert.AreEqual(createdUser.UserName, user.UserName);
        }
    }
    [TestMethod()]
    public async Task RegistrationDate()
    {
        var db = DbHelper.GetEmptyTestDB();

        using (var dbContext = new MemCheckDbContext(db))
        {
            using var userManager = UserHelper.GetUserManager(dbContext);
            var identityResult = await userManager.CreateAsync(UserHelper.Create());
            Assert.IsTrue(identityResult.Succeeded);
        }

        using (var dbContext = new MemCheckDbContext(db))
        {
            var user = await dbContext.Users.SingleAsync();
            Assert.IsTrue(DateTime.UtcNow - user.RegistrationUtcDate < TimeSpan.FromMinutes(10));
        }
    }
    [DataTestMethod, DataRow(" "), DataRow("     "), DataRow("A4!fi")]
    public async Task PasswordTooShort(string password)
    {
        var db = DbHelper.GetEmptyTestDB();

        using (var dbContext = new MemCheckDbContext(db))
        using (var userManager = UserHelper.GetUserManager(dbContext))
        {
            var user = UserHelper.Create();
            var result = await userManager.CreateAsync(user, password);
            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Errors.Any(error => error.Code == nameof(IdentityErrorDescriber.PasswordTooShort)));
        }

        using (var dbContext = new MemCheckDbContext(db))
            Assert.AreEqual(0, dbContext.Users.Count());
    }
    [DataTestMethod, DataRow("AaHJK@HGKHJHh")]
    public async Task PasswordWithoutDigit(string password)
    {
        var db = DbHelper.GetEmptyTestDB();

        using (var dbContext = new MemCheckDbContext(db))
        using (var userManager = UserHelper.GetUserManager(dbContext))
        {
            var user = UserHelper.Create();
            var result = await userManager.CreateAsync(user, password);
            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Errors.Any(error => error.Code == nameof(IdentityErrorDescriber.PasswordRequiresDigit)));
        }

        using (var dbContext = new MemCheckDbContext(db))
            Assert.AreEqual(0, dbContext.Users.Count());
    }
    [DataTestMethod, DataRow("Aa28781")]
    public async Task PasswordWithoutNonAlphanumeric(string password)
    {
        var db = DbHelper.GetEmptyTestDB();

        using (var dbContext = new MemCheckDbContext(db))
        using (var userManager = UserHelper.GetUserManager(dbContext))
        {
            var user = UserHelper.Create();
            var result = await userManager.CreateAsync(user, password);
            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Errors.Any(error => error.Code == nameof(IdentityErrorDescriber.PasswordRequiresNonAlphanumeric)));
        }

        using (var dbContext = new MemCheckDbContext(db))
            Assert.AreEqual(0, dbContext.Users.Count());
    }
    [DataTestMethod, DataRow("A28781B")]
    public async Task PasswordWithoutLower(string password)
    {
        var db = DbHelper.GetEmptyTestDB();

        using (var dbContext = new MemCheckDbContext(db))
        using (var userManager = UserHelper.GetUserManager(dbContext))
        {
            var user = UserHelper.Create();
            var result = await userManager.CreateAsync(user, password);
            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Errors.Any(error => error.Code == nameof(IdentityErrorDescriber.PasswordRequiresLower)));
        }

        using (var dbContext = new MemCheckDbContext(db))
            Assert.AreEqual(0, dbContext.Users.Count());
    }
    [DataTestMethod, DataRow("hdqskqhkjdsh")]
    public async Task PasswordWithoutUpper(string password)
    {
        var db = DbHelper.GetEmptyTestDB();

        using (var dbContext = new MemCheckDbContext(db))
        using (var userManager = UserHelper.GetUserManager(dbContext))
        {
            var user = UserHelper.Create();
            var result = await userManager.CreateAsync(user, password);
            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Errors.Any(error => error.Code == nameof(IdentityErrorDescriber.PasswordRequiresUpper)));
        }

        using (var dbContext = new MemCheckDbContext(db))
            Assert.AreEqual(0, dbContext.Users.Count());
    }
    [DataTestMethod, DataRow("toto"), DataRow("with space"), DataRow("with multi spaces"), DataRow("Avec accent Érik"), DataRow("Avec cédille comme ça")]
    public async Task SuccessWithUserName(string userName)
    {
        var db = DbHelper.GetEmptyTestDB();

        var userToCreate = UserHelper.Create(userName: userName);

        using (var dbContext = new MemCheckDbContext(db))
        using (var userManager = UserHelper.GetUserManager(dbContext))
        {
            var creationResult = await userManager.CreateAsync(userToCreate, RandomHelper.Password());
            Assert.IsTrue(creationResult.Succeeded);
        }

        using (var dbContext = new MemCheckDbContext(db))
        using (var userManager = UserHelper.GetUserManager(dbContext))
        {
            var userFromDb = await userManager.FindByIdAsync(userToCreate.Id.ToString());
            Assert.IsNotNull(userFromDb);
            Assert.AreEqual(userToCreate.UserName, userFromDb.UserName);
            Assert.AreEqual(userToCreate.GetUserName().ToUpperInvariant(), userFromDb.NormalizedUserName);
            Assert.AreEqual(userToCreate.Email, userFromDb.Email);
            Assert.IsNull(userFromDb.DeletionDate);
            Assert.IsTrue(userFromDb.EmailConfirmed);
            Assert.IsFalse(userFromDb.LockoutEnabled);
            Assert.IsNull(userFromDb.LockoutEnd);
            Assert.IsNotNull(userFromDb.PasswordHash);

            var getUserDecks = new GetUserDecks(dbContext.AsCallContext());
            var userDecks = await getUserDecks.RunAsync(new GetUserDecks.Request(userToCreate.Id));
            Assert.AreEqual(1, userDecks.Count());
            var deck = userDecks.First();
            Assert.AreEqual(0, deck.CardCount);
            Assert.AreEqual(MemCheckUserManager.DefaultDeckName, deck.Description);
            Assert.AreEqual(HeapingAlgorithms.DefaultAlgoId, deck.HeapingAlgorithmId);
        }
    }
    [TestMethod()]
    public async Task TwoAccountsWithSameEmailSuccess()
    {
        var db = DbHelper.GetEmptyTestDB();

        var user1 = UserHelper.Create();

        using (var dbContext = new MemCheckDbContext(db))
        using (var userManager = UserHelper.GetUserManager(dbContext))
        {
            var creationResult = await userManager.CreateAsync(user1);
            Assert.IsTrue(creationResult.Succeeded);
        }

        var user2 = UserHelper.Create(email: user1.Email);
        using (var dbContext = new MemCheckDbContext(db))
        using (var userManager = UserHelper.GetUserManager(dbContext))
        {
            var creationResult = await userManager.CreateAsync(user2);
            Assert.IsTrue(creationResult.Succeeded);
        }

        using (var dbContext = new MemCheckDbContext(db))
        using (var userManager = UserHelper.GetUserManager(dbContext))
        {
            {
                var user1FromDb = await userManager.FindByIdAsync(user1.Id.ToString());
                Assert.IsNotNull(user1FromDb);
                Assert.AreEqual(user1.UserName, user1FromDb.UserName);
                Assert.AreEqual(user1.GetUserName().ToUpperInvariant(), user1FromDb.NormalizedUserName);
                Assert.AreEqual(user1.Email, user1FromDb.Email);
                Assert.IsNull(user1FromDb.DeletionDate);
                Assert.IsTrue(user1FromDb.EmailConfirmed);
                Assert.IsFalse(user1FromDb.LockoutEnabled);
                Assert.IsNull(user1FromDb.LockoutEnd);

                var user1Decks = await new GetUserDecks(dbContext.AsCallContext()).RunAsync(new GetUserDecks.Request(user1.Id));
                Assert.AreEqual(1, user1Decks.Count());
                var user1Deck = user1Decks.First();
                Assert.AreEqual(0, user1Deck.CardCount);
                Assert.AreEqual(MemCheckUserManager.DefaultDeckName, user1Deck.Description);
                Assert.AreEqual(HeapingAlgorithms.DefaultAlgoId, user1Deck.HeapingAlgorithmId);
            }
            {
                var user2FromDb = await userManager.FindByIdAsync(user2.Id.ToString());
                Assert.IsNotNull(user2FromDb);
                Assert.AreEqual(user2.UserName, user2FromDb.UserName);
                Assert.AreEqual(user2.GetUserName().ToUpperInvariant(), user2FromDb.NormalizedUserName);
                Assert.AreEqual(user2.Email, user2FromDb.Email);
                Assert.IsNull(user2FromDb.DeletionDate);
                Assert.IsTrue(user2FromDb.EmailConfirmed);
                Assert.IsFalse(user2FromDb.LockoutEnabled);
                Assert.IsNull(user2FromDb.LockoutEnd);

                var user2Decks = await new GetUserDecks(dbContext.AsCallContext()).RunAsync(new GetUserDecks.Request(user2.Id));
                Assert.AreEqual(1, user2Decks.Count());
                var user1Deck = user2Decks.First();
                Assert.AreEqual(0, user1Deck.CardCount);
                Assert.AreEqual(MemCheckUserManager.DefaultDeckName, user1Deck.Description);
                Assert.AreEqual(HeapingAlgorithms.DefaultAlgoId, user1Deck.HeapingAlgorithmId);
            }
        }
    }
    //[DataTestMethod, DataRow(""), DataRow("gggg"), DataRow("@x"), DataRow("@x.com"), DataRow("dsq@")]
    //public async Task InvalidEmail(string email)
    //{
    //    var db = DbHelper.GetEmptyTestDB();

    //    using (var dbContext = new MemCheckDbContext(db))
    //    {
    //        using var userManager = UserHelper.GetUserManager(dbContext);
    //        var user = UserHelper.Create(email: email);
    //        var identityResult = await userManager.CreateAsync(user);
    //        Assert.IsFalse(identityResult.Succeeded);
    //        Assert.AreEqual(nameof(IdentityErrorDescriber.InvalidEmail), identityResult.Errors.Single().Code);
    //    }

    //    using (var dbContext = new MemCheckDbContext(db))
    //        Assert.IsFalse(await dbContext.Users.AnyAsync());
    //}
}
