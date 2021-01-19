using MemCheck.Application.Heaping;
using MemCheck.Application.QueryValidation;
using MemCheck.Application.Tests.Helpers;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.DeckChanging
{
    [TestClass()]
    public class UpdateDeckTests
    {
        [TestMethod()]
        public async Task UserNotLoggedIn()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);

            using var dbContext = new MemCheckDbContext(db);
            var request = new UpdateDeck.Request(Guid.Empty, deck, StringHelper.RandomString(), 0);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new UpdateDeck(dbContext).RunAsync(request, new TestLocalizer()));
        }
        [TestMethod()]
        public async Task UserDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);

            using var dbContext = new MemCheckDbContext(db);
            var request = new UpdateDeck.Request(Guid.NewGuid(), deck, StringHelper.RandomString(), 0);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new UpdateDeck(dbContext).RunAsync(request, new TestLocalizer()));
        }
        [TestMethod()]
        public async Task DeckDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            var request = new UpdateDeck.Request(user, Guid.NewGuid(), StringHelper.RandomString(), 0);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new UpdateDeck(dbContext).RunAsync(request, new TestLocalizer()));
        }
        [TestMethod()]
        public async Task UserNotOwner()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);
            var otherUser = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            var request = new UpdateDeck.Request(otherUser, deck, StringHelper.RandomString(), RandomHelper.HeapingAlgorithm());
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateDeck(dbContext).RunAsync(request, new TestLocalizer()));
        }
        [TestMethod()]
        public async Task NameNotTrimmed()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);

            using var dbContext = new MemCheckDbContext(db);
            var request = new UpdateDeck.Request(user, deck, StringHelper.RandomString() + '\t', 0);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new UpdateDeck(dbContext).RunAsync(request, new TestLocalizer()));
        }
        [TestMethod()]
        public async Task NameTooShort()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);

            using var dbContext = new MemCheckDbContext(db);
            var request = new UpdateDeck.Request(user, deck, StringHelper.RandomString(UpdateDeck.Request.MinNameLength - 1), 0);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateDeck(dbContext).RunAsync(request, new TestLocalizer()));
        }
        [TestMethod()]
        public async Task NameTooLong()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);

            using var dbContext = new MemCheckDbContext(db);
            var request = new UpdateDeck.Request(user, deck, StringHelper.RandomString(UpdateDeck.Request.MaxNameLength + 1), 0);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateDeck(dbContext).RunAsync(request, new TestLocalizer()));
        }
        [TestMethod()]
        public async Task DeckWithThisNameExists()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);
            var otherDeckName = StringHelper.RandomString();
            await DeckHelper.CreateAsync(db, user, otherDeckName);

            using var dbContext = new MemCheckDbContext(db);
            var request = new UpdateDeck.Request(user, deck, otherDeckName, RandomHelper.HeapingAlgorithm());
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateDeck(dbContext).RunAsync(request, new TestLocalizer()));
        }
        [TestMethod()]
        public async Task InexistentAlgorithm()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);

            using var dbContext = new MemCheckDbContext(db);
            var request = new UpdateDeck.Request(user, deck, StringHelper.RandomString(), RandomHelper.ValueNotInSet(HeapingAlgorithms.Instance.Ids));
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new UpdateDeck(dbContext).RunAsync(request, new TestLocalizer()));
        }
        [TestMethod()]
        public async Task FieldsCorrectlyUpdated()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);

            var newName = StringHelper.RandomString();
            int newAlgo = RandomHelper.HeapingAlgorithm();
            var request = new UpdateDeck.Request(user, deck, newName, newAlgo);

            using (var dbContext = new MemCheckDbContext(db))
                await new UpdateDeck(dbContext).RunAsync(request, new TestLocalizer());

            using (var dbContext = new MemCheckDbContext(db))
            {
                var deckFromDb = await dbContext.Decks.SingleAsync(d => d.Id == deck);
                Assert.AreEqual(newName, deckFromDb.Description);
                Assert.AreEqual(newAlgo, deckFromDb.HeapingAlgorithmId);
            }
        }
        [TestMethod()]
        public async Task FieldsNotAltered()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);
            var card = await CardHelper.CreateAsync(db, user);
            await DeckHelper.AddCardAsync(db, user, deck, card.Id, RandomHelper.RandomHeap());

            var request = new UpdateDeck.Request(user, deck, StringHelper.RandomString(), RandomHelper.HeapingAlgorithm());

            using (var dbContext = new MemCheckDbContext(db))
                await new UpdateDeck(dbContext).RunAsync(request, new TestLocalizer());

            using (var dbContext = new MemCheckDbContext(db))
            {
                var deckFromDb = await dbContext.Decks.Include(d => d.Owner).Include(d => d.CardInDecks).SingleAsync(d => d.Id == deck);
                Assert.AreEqual(user, deckFromDb.Owner.Id);
                Assert.AreEqual(card.Id, deckFromDb.CardInDecks.Single().CardId);
            }
        }
    }
}
