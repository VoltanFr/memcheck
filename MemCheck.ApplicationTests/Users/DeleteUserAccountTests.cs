using MemCheck.Application.Helpers;
using MemCheck.Application.Notifiying;
using MemCheck.Application.Ratings;
using MemCheck.Basics;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.AspNetCore.Identity;
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
            using var userManager = UserHelper.GetUserManager(dbContext);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new DeleteUserAccount(dbContext.AsCallContext(), userManager).RunAsync(new DeleteUserAccount.Request(Guid.Empty, userToDelete)));
        }
        [TestMethod()]
        public async Task LoggedUserDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var userToDelete = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            using var userManager = UserHelper.GetUserManager(dbContext);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new DeleteUserAccount(dbContext.AsCallContext(), userManager).RunAsync(new DeleteUserAccount.Request(Guid.NewGuid(), userToDelete)));
        }
        [TestMethod()]
        public async Task LoggedUserIsNotAdmin()
        {
            var db = DbHelper.GetEmptyTestDB();
            var loggedUser = await UserHelper.CreateInDbAsync(db);
            var userToDelete = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            using var userManager = UserHelper.GetUserManager(dbContext);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new DeleteUserAccount(dbContext.AsCallContext(), userManager).RunAsync(new DeleteUserAccount.Request(loggedUser, userToDelete)));
        }
        [TestMethod()]
        public async Task UserToDeleteDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var loggedUser = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            using var userManager = UserHelper.GetUserManager(dbContext);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new DeleteUserAccount(dbContext.AsCallContext(), userManager).RunAsync(new DeleteUserAccount.Request(loggedUser, Guid.NewGuid())));
        }
        [TestMethod()]
        public async Task UserToDeleteIsAdmin()
        {
            var db = DbHelper.GetEmptyTestDB();
            var loggedUser = await UserHelper.CreateInDbAsync(db);
            var userToDelete = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            using var userManager = UserHelper.GetUserManager(dbContext);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new DeleteUserAccount(dbContext.AsCallContext(new TestRoleChecker(loggedUser, userToDelete)), userManager).RunAsync(new DeleteUserAccount.Request(loggedUser, userToDelete)));
        }
        [TestMethod()]
        public async Task DeletionByAdmin()
        {
            var db = DbHelper.GetEmptyTestDB();
            var loggedUser = await UserHelper.CreateInDbAsync(db);

            var userToDeleteName = RandomHelper.String();
            var userToDeleteEmail = RandomHelper.String();
            var userToDeleteId = await UserHelper.CreateInDbAsync(db, userName: userToDeleteName, userEMail: userToDeleteEmail);
            await UserHelper.SetRandomPasswordAsync(db, userToDeleteId);
            var runDate = RandomHelper.Date();

            using (var dbContext = new MemCheckDbContext(db))
            //Check user is all set
            {
                MemCheckUser userToDelete = dbContext.Users.Single(u => u.Id == userToDeleteId);
                Assert.AreEqual(userToDeleteName, userToDelete.UserName);
                Assert.AreEqual(userToDeleteName.ToUpperInvariant(), userToDelete.NormalizedUserName);
                Assert.AreEqual(userToDeleteEmail, userToDelete.Email);
                Assert.IsTrue(userToDelete.EmailConfirmed);
                Assert.IsFalse(userToDelete.LockoutEnabled);
                Assert.IsNull(userToDelete.LockoutEnd);
                Assert.IsNull(userToDelete.DeletionDate);
                Assert.IsNotNull(userToDelete.PasswordHash);
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                using var userManager = UserHelper.GetUserManager(dbContext);
                await new DeleteUserAccount(dbContext.AsCallContext(new TestRoleChecker(loggedUser)), userManager, runDate).RunAsync(new DeleteUserAccount.Request(loggedUser, userToDeleteId));
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var deletedUser = await dbContext.Users.SingleAsync(u => u.Id == userToDeleteId);
                Assert.AreEqual(DeleteUserAccount.DeletedUserName, deletedUser.UserName);
                Assert.AreEqual(userToDeleteName.ToUpperInvariant(), deletedUser.NormalizedUserName);
                Assert.AreEqual(DeleteUserAccount.DeletedUserEmail, deletedUser.Email);
                Assert.IsFalse(deletedUser.EmailConfirmed);
                Assert.IsTrue(deletedUser.LockoutEnabled);
                Assert.AreEqual(DateTime.MaxValue, deletedUser.LockoutEnd);
                Assert.AreEqual(runDate, deletedUser.DeletionDate);
                Assert.IsNull(deletedUser.PasswordHash);
            }
        }
        [TestMethod()]
        public async Task DeletionByUserHimself()
        {
            var db = DbHelper.GetEmptyTestDB();

            var userToDeleteName = RandomHelper.String();
            var userToDeleteEmail = RandomHelper.String();
            var userToDeleteId = await UserHelper.CreateInDbAsync(db, userName: userToDeleteName, userEMail: userToDeleteEmail);
            await UserHelper.SetRandomPasswordAsync(db, userToDeleteId);
            var runDate = RandomHelper.Date();

            using (var dbContext = new MemCheckDbContext(db))
            //Check user is all set
            {
                MemCheckUser userToDelete = dbContext.Users.Single(u => u.Id == userToDeleteId);
                Assert.AreEqual(userToDeleteName, userToDelete.UserName);
                Assert.AreEqual(userToDeleteName.ToUpperInvariant(), userToDelete.NormalizedUserName);
                Assert.AreEqual(userToDeleteEmail, userToDelete.Email);
                Assert.IsTrue(userToDelete.EmailConfirmed);
                Assert.IsFalse(userToDelete.LockoutEnabled);
                Assert.IsNull(userToDelete.LockoutEnd);
                Assert.IsNull(userToDelete.DeletionDate);
                Assert.IsNotNull(userToDelete.PasswordHash);
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                using var userManager = UserHelper.GetUserManager(dbContext);
                await new DeleteUserAccount(dbContext.AsCallContext(), userManager, runDate).RunAsync(new DeleteUserAccount.Request(userToDeleteId, userToDeleteId));
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var deletedUser = await dbContext.Users.SingleAsync(u => u.Id == userToDeleteId);
                Assert.AreEqual(DeleteUserAccount.DeletedUserName, deletedUser.UserName);
                Assert.AreEqual(userToDeleteName.ToUpperInvariant(), deletedUser.NormalizedUserName);
                Assert.AreEqual(DeleteUserAccount.DeletedUserEmail, deletedUser.Email);
                Assert.IsFalse(deletedUser.EmailConfirmed);
                Assert.IsTrue(deletedUser.LockoutEnabled);
                Assert.AreEqual(DateTime.MaxValue, deletedUser.LockoutEnd);
                Assert.AreEqual(runDate, deletedUser.DeletionDate);
                Assert.IsNull(deletedUser.PasswordHash);
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
            {
                using var userManager = UserHelper.GetUserManager(dbContext);
                await new DeleteUserAccount(dbContext.AsCallContext(new TestRoleChecker(loggedUser)), userManager).RunAsync(new DeleteUserAccount.Request(loggedUser, userToDelete));
            }

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
            {
                using var userManager = UserHelper.GetUserManager(dbContext);
                await new DeleteUserAccount(dbContext.AsCallContext(new TestRoleChecker(loggedUser)), userManager).RunAsync(new DeleteUserAccount.Request(loggedUser, userToDelete));
            }

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
            {
                using var userManager = UserHelper.GetUserManager(dbContext);
                await new DeleteUserAccount(dbContext.AsCallContext(new TestRoleChecker(loggedUser)), userManager).RunAsync(new DeleteUserAccount.Request(loggedUser, userToDelete));
            }

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
                await new SetCardRating(dbContext.AsCallContext()).RunAsync(new SetCardRating.Request(loggedUser, card.Id, 3));
                await new SetCardRating(dbContext.AsCallContext()).RunAsync(new SetCardRating.Request(userToDelete, card.Id, 3));
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                using var userManager = UserHelper.GetUserManager(dbContext);
                await new DeleteUserAccount(dbContext.AsCallContext(new TestRoleChecker(loggedUser)), userManager).RunAsync(new DeleteUserAccount.Request(loggedUser, userToDelete));
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                Assert.IsFalse(await dbContext.UserCardRatings.AnyAsync(rating => rating.UserId == userToDelete));
                Assert.IsTrue(await dbContext.UserCardRatings.AnyAsync(rating => rating.UserId == loggedUser));
            }
        }
        [TestMethod()]
        public async Task SearchSubscriptions()
        {
            var db = DbHelper.GetEmptyTestDB();
            var loggedUser = await UserHelper.CreateInDbAsync(db);
            var userToDelete = await UserHelper.CreateInDbAsync(db);
            var tag = await TagHelper.CreateAsync(db);
            await CardHelper.CreateAsync(db, userToDelete, tagIds: tag.AsArray());
            await CardHelper.CreateAsync(db, userToDelete);
            await CardHelper.CreateAsync(db, loggedUser, tagIds: tag.AsArray());

            Guid loggedUserSubscriptionId;
            Guid userToDeleteSubscriptionId;
            using (var dbContext = new MemCheckDbContext(db))
            {
                loggedUserSubscriptionId = (await new SubscribeToSearch(dbContext.AsCallContext()).RunAsync(new SubscribeToSearch.Request(loggedUser, Guid.Empty, RandomHelper.String(), "", tag.AsArray(), Array.Empty<Guid>()))).SearchId;
                userToDeleteSubscriptionId = (await new SubscribeToSearch(dbContext.AsCallContext()).RunAsync(new SubscribeToSearch.Request(userToDelete, Guid.Empty, RandomHelper.String(), "", tag.AsArray(), Array.Empty<Guid>()))).SearchId;
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                await new UserSearchNotifier(dbContext.AsCallContext(), 10, new DateTime(2050, 05, 01)).RunAsync(loggedUserSubscriptionId);
                await new UserSearchNotifier(dbContext.AsCallContext(), 10, new DateTime(2050, 05, 01)).RunAsync(userToDeleteSubscriptionId);
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                Assert.AreEqual(2, dbContext.CardsInSearchResults.Count(c => c.SearchSubscriptionId == loggedUserSubscriptionId));
                Assert.AreEqual(2, dbContext.CardsInSearchResults.Count(c => c.SearchSubscriptionId == userToDeleteSubscriptionId));
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                using var userManager = UserHelper.GetUserManager(dbContext);
                await new DeleteUserAccount(dbContext.AsCallContext(new TestRoleChecker(loggedUser)), userManager).RunAsync(new DeleteUserAccount.Request(loggedUser, userToDelete));
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                Assert.AreEqual(1, await dbContext.SearchSubscriptions.CountAsync(sub => sub.Id == loggedUserSubscriptionId));
                Assert.AreEqual(0, await dbContext.SearchSubscriptions.CountAsync(sub => sub.Id == userToDeleteSubscriptionId));

                Assert.AreEqual(1, await dbContext.RequiredTagInSearchSubscriptions.CountAsync(reqTag => reqTag.SearchSubscriptionId == loggedUserSubscriptionId));
                Assert.AreEqual(0, await dbContext.RequiredTagInSearchSubscriptions.CountAsync(reqTag => reqTag.SearchSubscriptionId == userToDeleteSubscriptionId));

                Assert.AreEqual(2, await dbContext.CardsInSearchResults.CountAsync(cardInSearchResult => cardInSearchResult.SearchSubscriptionId == loggedUserSubscriptionId));
                Assert.AreEqual(0, await dbContext.CardsInSearchResults.CountAsync(cardInSearchResult => cardInSearchResult.SearchSubscriptionId == userToDeleteSubscriptionId));
            }
        }
        [TestMethod()]
        public async Task NonPrivateCardNotDeleted_UserOwnerOfCurrentAndPrevious()
        {
            var db = DbHelper.GetEmptyTestDB();
            var loggedUser = await UserHelper.CreateInDbAsync(db);
            var language = await CardLanguagHelper.CreateAsync(db);
            var userToDelete = await UserHelper.CreateInDbAsync(db);
            var card = await CardHelper.CreateAsync(db, userToDelete, language: language);
            await UpdateCardHelper.RunAsync(db, UpdateCardHelper.RequestForFrontSideChange(card, RandomHelper.String()));

            using (var dbContext = new MemCheckDbContext(db))
            {
                using var userManager = UserHelper.GetUserManager(dbContext);
                await new DeleteUserAccount(dbContext.AsCallContext(new TestRoleChecker(loggedUser)), userManager).RunAsync(new DeleteUserAccount.Request(loggedUser, userToDelete));
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                Assert.AreEqual(DeleteUserAccount.DeletedUserName, dbContext.Cards.Include(card => card.VersionCreator).Single(card => card.Id == card.Id).VersionCreator.UserName);
                Assert.AreEqual(DeleteUserAccount.DeletedUserName, dbContext.CardPreviousVersions.Single(cardPreviousVersion => cardPreviousVersion.Card == card.Id).VersionCreator.UserName);
            }
        }
        [TestMethod()]
        public async Task NonPrivateCardNotDeleted_UserNotOwnerOfCurrentButOfPrevious()
        {
            var db = DbHelper.GetEmptyTestDB();
            var loggedUserName = RandomHelper.String();
            var loggedUser = await UserHelper.CreateInDbAsync(db, userName: loggedUserName);
            var language = await CardLanguagHelper.CreateAsync(db);
            var userToDelete = await UserHelper.CreateInDbAsync(db);
            var card = await CardHelper.CreateAsync(db, userToDelete, language: language, userWithViewIds: new[] { loggedUser, userToDelete });
            await UpdateCardHelper.RunAsync(db, UpdateCardHelper.RequestForFrontSideChange(card, RandomHelper.String(), versionCreator: loggedUser));

            using (var dbContext = new MemCheckDbContext(db))
            {
                using var userManager = UserHelper.GetUserManager(dbContext);
                await new DeleteUserAccount(dbContext.AsCallContext(new TestRoleChecker(loggedUser)), userManager).RunAsync(new DeleteUserAccount.Request(loggedUser, userToDelete));
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                Assert.AreEqual(loggedUserName, dbContext.Cards.Include(card => card.VersionCreator).Single(card => card.Id == card.Id).VersionCreator.UserName);
                Assert.AreEqual(DeleteUserAccount.DeletedUserName, dbContext.CardPreviousVersions.Include(cardPreviousVersion => cardPreviousVersion.VersionCreator).Single(cardPreviousVersion => cardPreviousVersion.Card == card.Id).VersionCreator.UserName);
            }
        }
        [TestMethod()]
        public async Task NonPrivateCardNotDeleted_UserOwnerOfCurrentButNotPrevious()
        {
            var db = DbHelper.GetEmptyTestDB();
            var loggedUserName = RandomHelper.String();
            var loggedUser = await UserHelper.CreateInDbAsync(db, userName: loggedUserName);
            var language = await CardLanguagHelper.CreateAsync(db);
            var userToDelete = await UserHelper.CreateInDbAsync(db);
            var card = await CardHelper.CreateAsync(db, loggedUser, language: language);
            await UpdateCardHelper.RunAsync(db, UpdateCardHelper.RequestForFrontSideChange(card, RandomHelper.String(), versionCreator: userToDelete));

            using (var dbContext = new MemCheckDbContext(db))
            {
                using var userManager = UserHelper.GetUserManager(dbContext);
                await new DeleteUserAccount(dbContext.AsCallContext(new TestRoleChecker(loggedUser)), userManager).RunAsync(new DeleteUserAccount.Request(loggedUser, userToDelete));
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                Assert.AreEqual(DeleteUserAccount.DeletedUserName, dbContext.Cards.Include(card => card.VersionCreator).Single(card => card.Id == card.Id).VersionCreator.UserName);
                Assert.AreEqual(loggedUserName, dbContext.CardPreviousVersions.Include(cardPreviousVersion => cardPreviousVersion.VersionCreator).Single(cardPreviousVersion => cardPreviousVersion.Card == card.Id).VersionCreator.UserName);
            }
        }
        [TestMethod()]
        public async Task PrivateCardDeleted()
        {
            var db = DbHelper.GetEmptyTestDB();
            var loggedUser = await UserHelper.CreateInDbAsync(db);
            var language = await CardLanguagHelper.CreateAsync(db);
            var userToDelete = await UserHelper.CreateInDbAsync(db);
            var card = await CardHelper.CreateAsync(db, userToDelete, language: language);  //Public
            await UpdateCardHelper.RunAsync(db, UpdateCardHelper.RequestForVisibilityChange(card, userWithViewIds: userToDelete.AsArray())); //Private

            using (var dbContext = new MemCheckDbContext(db))
            {
                using var userManager = UserHelper.GetUserManager(dbContext);
                await new DeleteUserAccount(dbContext.AsCallContext(new TestRoleChecker(loggedUser)), userManager).RunAsync(new DeleteUserAccount.Request(loggedUser, userToDelete));
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                Assert.IsFalse(dbContext.Cards.Any());
                Assert.IsFalse(dbContext.CardPreviousVersions.Any());
            }
        }
        [TestMethod()]
        public async Task PrivateCardDeletedWhileOthersExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var loggedUser = await UserHelper.CreateInDbAsync(db);
            var language = await CardLanguagHelper.CreateAsync(db);
            var userToDelete = await UserHelper.CreateInDbAsync(db);
            var privateCardToDelete = await CardHelper.CreateAsync(db, userToDelete, language: language, userWithViewIds: userToDelete.AsArray());
            await UpdateCardHelper.RunAsync(db, UpdateCardHelper.RequestForBackSideChange(privateCardToDelete, RandomHelper.String()));
            var privateCardNotToDelete = await CardHelper.CreateAsync(db, loggedUser, language: language, userWithViewIds: loggedUser.AsArray());
            await UpdateCardHelper.RunAsync(db, UpdateCardHelper.RequestForBackSideChange(privateCardNotToDelete, RandomHelper.String()));
            var publicCard = await CardHelper.CreateAsync(db, userToDelete, language: language);
            await UpdateCardHelper.RunAsync(db, UpdateCardHelper.RequestForBackSideChange(publicCard, RandomHelper.String()));

            using (var dbContext = new MemCheckDbContext(db))
            {
                using var userManager = UserHelper.GetUserManager(dbContext);
                await new DeleteUserAccount(dbContext.AsCallContext(new TestRoleChecker(loggedUser)), userManager).RunAsync(new DeleteUserAccount.Request(loggedUser, userToDelete));
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                Assert.IsFalse(dbContext.Cards.Any(card => card.Id == privateCardToDelete.Id));
                Assert.IsFalse(dbContext.CardPreviousVersions.Any(previousVersion => previousVersion.Card == privateCardToDelete.Id));
                Assert.IsTrue(dbContext.Cards.Any(card => card.Id == privateCardNotToDelete.Id));
                Assert.IsTrue(dbContext.CardPreviousVersions.Any(previousVersion => previousVersion.Card == privateCardNotToDelete.Id));
                Assert.IsTrue(dbContext.Cards.Any(card => card.Id == publicCard.Id));
                Assert.IsTrue(dbContext.CardPreviousVersions.Any(previousVersion => previousVersion.Card == publicCard.Id));
            }
        }
        [TestMethod()]
        public async Task LimitedCardVisibilityUpdated()
        {
            var db = DbHelper.GetEmptyTestDB();
            var loggedUser = await UserHelper.CreateInDbAsync(db);
            var cardCreator = await UserHelper.CreateInDbAsync(db);
            var userToDelete = await UserHelper.CreateInDbAsync(db);
            var cardId = await CardHelper.CreateIdAsync(db, cardCreator, userWithViewIds: new[] { loggedUser, cardCreator, userToDelete });

            using (var dbContext = new MemCheckDbContext(db))
            {
                using var userManager = UserHelper.GetUserManager(dbContext);
                await new DeleteUserAccount(dbContext.AsCallContext(new TestRoleChecker(loggedUser)), userManager).RunAsync(new DeleteUserAccount.Request(loggedUser, userToDelete));
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var card = await dbContext.Cards.Include(card => card.UsersWithView).SingleAsync(c => c.Id == cardId);
                Assert.AreEqual(2, card.UsersWithView.Count());
                Assert.IsFalse(card.UsersWithView.Any(userWithView => userWithView.UserId == userToDelete));
            }
        }
        [TestMethod()]
        public async Task PublicCardVisibilityNotUpdated()
        {
            var db = DbHelper.GetEmptyTestDB();
            var loggedUser = await UserHelper.CreateInDbAsync(db);
            var userToDelete = await UserHelper.CreateInDbAsync(db);
            var cardId = await CardHelper.CreateIdAsync(db, userToDelete);

            using (var dbContext = new MemCheckDbContext(db))
            {
                using var userManager = UserHelper.GetUserManager(dbContext);
                await new DeleteUserAccount(dbContext.AsCallContext(new TestRoleChecker(loggedUser)), userManager).RunAsync(new DeleteUserAccount.Request(loggedUser, userToDelete));
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var card = await dbContext.Cards.Include(card => card.UsersWithView).SingleAsync(c => c.Id == cardId);
                Assert.AreEqual(0, card.UsersWithView.Count());
            }
        }
        [TestMethod()]
        public async Task CardNotificationSubscriptions()
        {
            var db = DbHelper.GetEmptyTestDB();
            var loggedUser = await UserHelper.CreateInDbAsync(db);
            var userToDelete = await UserHelper.CreateInDbAsync(db);
            var tag = await TagHelper.CreateAsync(db);
            var card1 = await CardHelper.CreateAsync(db, loggedUser, tagIds: tag.AsArray());
            var card2 = await CardHelper.CreateAsync(db, userToDelete);
            var card3 = await CardHelper.CreateAsync(db, loggedUser, tagIds: tag.AsArray());

            using (var dbContext = new MemCheckDbContext(db))
            {
                await new AddCardSubscriptions(dbContext.AsCallContext()).RunAsync(new AddCardSubscriptions.Request(userToDelete, new Guid[] { card1.Id, card2.Id }));
                await new AddCardSubscriptions(dbContext.AsCallContext()).RunAsync(new AddCardSubscriptions.Request(loggedUser, new Guid[] { card2.Id, card3.Id }));
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                Assert.AreEqual(2, dbContext.CardNotifications.Count(cardNotifSubscription => cardNotifSubscription.UserId == loggedUser));
                Assert.AreEqual(2, dbContext.CardNotifications.Count(cardNotifSubscription => cardNotifSubscription.UserId == userToDelete));
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                using var userManager = UserHelper.GetUserManager(dbContext);
                await new DeleteUserAccount(dbContext.AsCallContext(new TestRoleChecker(loggedUser)), userManager).RunAsync(new DeleteUserAccount.Request(loggedUser, userToDelete));
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                Assert.AreEqual(2, dbContext.CardNotifications.Count(cardNotifSubscription => cardNotifSubscription.UserId == loggedUser));
                Assert.AreEqual(0, dbContext.CardNotifications.Count(cardNotifSubscription => cardNotifSubscription.UserId == userToDelete));
            }
        }
        [TestMethod()]
        public async Task UserNameCanNotBeReused()
        {
            //See comment on class DeleteUserAccount about why I don't want user names to be reused

            var db = DbHelper.GetEmptyTestDB();

            var userName = RandomHelper.String();
            var userToDeleteId = await UserHelper.CreateInDbAsync(db, userName: userName);

            using (var dbContext = new MemCheckDbContext(db))
            {
                using var userManager = UserHelper.GetUserManager(dbContext);
                await new DeleteUserAccount(dbContext.AsCallContext(), userManager).RunAsync(new DeleteUserAccount.Request(userToDeleteId, userToDeleteId));
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                Assert.AreEqual(1, dbContext.Users.Count());

                var user = dbContext.Users.Single();
                Assert.AreEqual(userToDeleteId, user.Id);
                Assert.AreNotEqual(userName, user.UserName);
                Assert.AreEqual(DeleteUserAccount.DeletedUserName, user.UserName);
                Assert.AreEqual(userName.ToUpperInvariant(), user.NormalizedUserName);
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                using var userManager = UserHelper.GetUserManager(dbContext);
                var user = new MemCheckUser
                {
                    UserName = userName
                };
                var creationResult = await userManager.CreateAsync(user);
                Assert.AreNotEqual(IdentityResult.Success, creationResult);
            }
        }
    }
}
