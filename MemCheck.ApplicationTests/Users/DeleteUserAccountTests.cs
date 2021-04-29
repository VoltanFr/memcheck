using MemCheck.Application.Ratings;
using MemCheck.Application.Tests.Helpers;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Users
{
    [TestClass()]
    public class DeleteUserAccountTests
    {
        [TestMethod()]
        public async Task UserNotLoggedIn()
        {
            var db = DbHelper.GetEmptyTestDB();
            var userToDelete = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new DeleteUserAccount(dbContext, new TestRoleChecker()).RunAsync(new DeleteUserAccount.Request(Guid.Empty, userToDelete)));
        }
        [TestMethod()]
        public async Task LoggedUserDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var userToDelete = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new DeleteUserAccount(dbContext, new TestRoleChecker()).RunAsync(new DeleteUserAccount.Request(Guid.NewGuid(), userToDelete)));
        }
        [TestMethod()]
        public async Task LoggedUserIsNotAdmin()
        {
            var db = DbHelper.GetEmptyTestDB();
            var loggedUser = await UserHelper.CreateInDbAsync(db);
            var userToDelete = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new DeleteUserAccount(dbContext, new TestRoleChecker()).RunAsync(new DeleteUserAccount.Request(loggedUser, userToDelete)));
        }
        [TestMethod()]
        public async Task UserToDeleteDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var loggedUser = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new DeleteUserAccount(dbContext, new TestRoleChecker(loggedUser)).RunAsync(new DeleteUserAccount.Request(loggedUser, Guid.NewGuid())));
        }
        [TestMethod()]
        public async Task UserToDeleteIsAdmin()
        {
            var db = DbHelper.GetEmptyTestDB();
            var loggedUser = await UserHelper.CreateInDbAsync(db);
            var userToDelete = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new DeleteUserAccount(dbContext, new TestRoleChecker(loggedUser, userToDelete)).RunAsync(new DeleteUserAccount.Request(loggedUser, userToDelete)));
        }
        [TestMethod()]
        public async Task UserAccountGetsAnonymized()
        {
            var db = DbHelper.GetEmptyTestDB();
            var loggedUser = await UserHelper.CreateInDbAsync(db);

            var userToDeleteName = RandomHelper.String();
            var userToDeleteEmail = RandomHelper.String();
            var userToDelete = await UserHelper.CreateInDbAsync(db, userName: userToDeleteName, userEMail: userToDeleteEmail);

            using (var dbContext = new MemCheckDbContext(db))
                await new DeleteUserAccount(dbContext, new TestRoleChecker(loggedUser)).RunAsync(new DeleteUserAccount.Request(loggedUser, userToDelete));

            using (var dbContext = new MemCheckDbContext(db))
            {
                var deletedUser = await dbContext.Users.SingleAsync(u => u.Id == userToDelete);
                Assert.AreEqual(DeleteUserAccount.DeletedUserName, deletedUser.UserName);
                Assert.AreEqual(DeleteUserAccount.DeletedUserEmail, deletedUser.Email);
                Assert.IsFalse(deletedUser.EmailConfirmed);
                Assert.IsTrue(deletedUser.LockoutEnabled);
                Assert.AreEqual(DateTime.MaxValue, deletedUser.LockoutEnd);
            }
        }
        [TestMethod()]
        public async Task UserEmptyDecks()
        {
            var db = DbHelper.GetEmptyTestDB();
            var loggedUser = await UserHelper.CreateInDbAsync(db);
            var userToDelete = await UserHelper.CreateInDbAsync(db);
            var deck1 = await DeckHelper.CreateAsync(db, userToDelete);
            var deck2 = await DeckHelper.CreateAsync(db, userToDelete);

            using (var dbContext = new MemCheckDbContext(db))
                await new DeleteUserAccount(dbContext, new TestRoleChecker(loggedUser)).RunAsync(new DeleteUserAccount.Request(loggedUser, userToDelete));

            using (var dbContext = new MemCheckDbContext(db))
            {
                Assert.IsFalse(await dbContext.Decks.AnyAsync(d => d.Id == deck1));
                Assert.IsFalse(await dbContext.Decks.AnyAsync(d => d.Id == deck2));
            }
        }
        [TestMethod()]
        public async Task UserDeckNotEmpty()
        {
            var db = DbHelper.GetEmptyTestDB();
            var loggedUser = await UserHelper.CreateInDbAsync(db);
            var userToDelete = await UserHelper.CreateInDbAsync(db);
            var deckToDelete = await DeckHelper.CreateAsync(db, userToDelete);
            var card = await CardHelper.CreateAsync(db, userToDelete);
            await DeckHelper.AddCardAsync(db, deckToDelete, card.Id);

            using (var dbContext = new MemCheckDbContext(db))
                await new DeleteUserAccount(dbContext, new TestRoleChecker(loggedUser)).RunAsync(new DeleteUserAccount.Request(loggedUser, userToDelete));

            using (var dbContext = new MemCheckDbContext(db))
            {
                Assert.IsFalse(await dbContext.Decks.AnyAsync(deck => deck.Id == deckToDelete));
                Assert.IsFalse(await dbContext.CardsInDecks.AnyAsync());
            }
        }
        [TestMethod()]
        public async Task DoesNotDeleteOtherUserDeck()
        {
            var db = DbHelper.GetEmptyTestDB();
            var loggedUser = await UserHelper.CreateInDbAsync(db);
            var deckNotToDelete = await DeckHelper.CreateAsync(db, loggedUser);
            var userToDelete = await UserHelper.CreateInDbAsync(db);
            var deckToDelete = await DeckHelper.CreateAsync(db, userToDelete);
            var card = await CardHelper.CreateAsync(db, userToDelete);
            await DeckHelper.AddCardAsync(db, deckToDelete, card.Id);
            await DeckHelper.AddCardAsync(db, deckNotToDelete, card.Id);

            using (var dbContext = new MemCheckDbContext(db))
                await new DeleteUserAccount(dbContext, new TestRoleChecker(loggedUser)).RunAsync(new DeleteUserAccount.Request(loggedUser, userToDelete));

            using (var dbContext = new MemCheckDbContext(db))
            {
                Assert.IsFalse(await dbContext.Decks.AnyAsync(deck => deck.Id == deckToDelete));
                Assert.IsFalse(await dbContext.CardsInDecks.AnyAsync(cardInDeck => cardInDeck.Deck.Id == deckToDelete));

                Assert.IsTrue(await dbContext.Decks.AnyAsync(deck => deck.Id == deckNotToDelete));
                Assert.IsTrue(await dbContext.CardsInDecks.AnyAsync(cardInDeck => cardInDeck.Deck.Id == deckNotToDelete));
            }
        }
        [TestMethod()]
        public async Task Ratings()
        {
            var db = DbHelper.GetEmptyTestDB();
            var loggedUser = await UserHelper.CreateInDbAsync(db);
            var userToDelete = await UserHelper.CreateInDbAsync(db);
            var card = await CardHelper.CreateAsync(db, userToDelete);
            using (var dbContext = new MemCheckDbContext(db))
            {
                await new SetCardRating(dbContext).RunAsync(new SetCardRating.Request(loggedUser, card.Id, 3));
                await new SetCardRating(dbContext).RunAsync(new SetCardRating.Request(userToDelete, card.Id, 3));
            }

            using (var dbContext = new MemCheckDbContext(db))
                await new DeleteUserAccount(dbContext, new TestRoleChecker(loggedUser)).RunAsync(new DeleteUserAccount.Request(loggedUser, userToDelete));

            using (var dbContext = new MemCheckDbContext(db))
            {
                Assert.IsFalse(await dbContext.UserCardRatings.AnyAsync(rating => rating.UserId == userToDelete));
                Assert.IsTrue(await dbContext.UserCardRatings.AnyAsync(rating => rating.UserId == loggedUser));
            }
        }
    }
}
