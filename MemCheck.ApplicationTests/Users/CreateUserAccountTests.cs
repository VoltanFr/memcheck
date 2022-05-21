using MemCheck.Application.Decks;
using MemCheck.Application.Heaping;
using MemCheck.Application.Helpers;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Users;

[TestClass()]
public class CreateUserAccountTests
{
    #region Private
    private static string RandomPassword()
    {
        return RandomHelper.String().ToUpperInvariant() + RandomHelper.String();
    }
    #endregion
    [TestMethod()]
    public async Task NameTooShort()
    {
        var db = DbHelper.GetEmptyTestDB();

        using (var dbContext = new MemCheckDbContext(db))
        using (var userManager = UserHelper.GetUserManager(dbContext))
        {
            var user = new MemCheckUser { UserName = RandomHelper.String(MemCheckUserManager.MinUserNameLength - 1), Email = RandomHelper.String() };
            var result = await userManager.CreateAsync(user, RandomPassword());
            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(MemCheckUserManager.BadUserNameLengthErrorCode, result.Errors.First().Code);
        }

        using (var dbContext = new MemCheckDbContext(db))
            Assert.AreEqual(0, dbContext.Users.Count());
    }
    [TestMethod()]
    public async Task NameTooLong()
    {
        var db = DbHelper.GetEmptyTestDB();

        using (var dbContext = new MemCheckDbContext(db))
        using (var userManager = UserHelper.GetUserManager(dbContext))
        {
            var user = new MemCheckUser { UserName = RandomHelper.String(MemCheckUserManager.MaxUserNameLength + 1), Email = RandomHelper.String() };
            var result = await userManager.CreateAsync(user, RandomPassword());
            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(MemCheckUserManager.BadUserNameLengthErrorCode, result.Errors.First().Code);
        }

        using (var dbContext = new MemCheckDbContext(db))
            Assert.AreEqual(0, dbContext.Users.Count());
    }
    [TestMethod()]
    public async Task PasswordEmpty()
    {
        var db = DbHelper.GetEmptyTestDB();

        using (var dbContext = new MemCheckDbContext(db))
        using (var userManager = UserHelper.GetUserManager(dbContext))
        {
            var user = new MemCheckUser { UserName = RandomHelper.String(), Email = RandomHelper.String() };
            var result = await userManager.CreateAsync(user, "");
            Assert.IsFalse(result.Succeeded);
        }

        using (var dbContext = new MemCheckDbContext(db))
            Assert.AreEqual(0, dbContext.Users.Count());
    }
    [TestMethod()]
    public async Task Success()
    {
        var db = DbHelper.GetEmptyTestDB();

        string userName = RandomHelper.String(MemCheckUserManager.MaxUserNameLength);
        string email = RandomHelper.String();
        var userToCreate = new MemCheckUser { UserName = userName, Email = email };

        using (var dbContext = new MemCheckDbContext(db))
        using (var userManager = UserHelper.GetUserManager(dbContext))
        {
            var creationResult = await userManager.CreateAsync(userToCreate, RandomPassword());
            Assert.IsTrue(creationResult.Succeeded);
        }

        using (var dbContext = new MemCheckDbContext(db))
        using (var userManager = UserHelper.GetUserManager(dbContext))
        {
            var userFromDb = await userManager.FindByIdAsync(userToCreate.Id.ToString());
            Assert.IsNotNull(userFromDb);
            Assert.AreEqual(userToCreate.UserName, userFromDb.UserName);
            Assert.AreEqual(userToCreate.Email, userFromDb.Email);
            Assert.IsNull(userFromDb.DeletionDate);
            Assert.IsFalse(userFromDb.EmailConfirmed);

            var getUserDecks = new GetUserDecks(dbContext.AsCallContext());
            var userDecks = await getUserDecks.RunAsync(new GetUserDecks.Request(userToCreate.Id));
            Assert.AreEqual(1, userDecks.Count());
            var deck = userDecks.First();
            Assert.AreEqual(0, deck.CardCount);
            Assert.AreEqual(MemCheckUserManager.DefaultDeckName, deck.Description);
            Assert.AreEqual(HeapingAlgorithms.DefaultAlgoId, deck.HeapingAlgorithmId);
        }
    }
}
