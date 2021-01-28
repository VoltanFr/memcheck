using MemCheck.Application.Tests.Helpers;
using MemCheck.Basics;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace MemCheck.Application.Decks
{
    [TestClass()]
    public class RemoveCardsFromDeckTests
    {
        [TestMethod()]
        public async Task UserNotLoggedIn()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);
            var card = await CardHelper.CreateAsync(db, user);
            await DeckHelper.AddCardAsync(db, deck, card.Id);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new RemoveCardsFromDeck(dbContext).RunAsync(new RemoveCardsFromDeck.Request(Guid.Empty, deck, card.Id.AsArray())));
        }
        [TestMethod()]
        public async Task UserDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);
            var card = await CardHelper.CreateAsync(db, user);
            await DeckHelper.AddCardAsync(db, deck, card.Id);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new RemoveCardsFromDeck(dbContext).RunAsync(new RemoveCardsFromDeck.Request(Guid.NewGuid(), deck, card.Id.AsArray())));
        }
        [TestMethod()]
        public async Task DeckDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var card = await CardHelper.CreateAsync(db, user);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new RemoveCardsFromDeck(dbContext).RunAsync(new RemoveCardsFromDeck.Request(user, Guid.NewGuid(), card.Id.AsArray())));
        }
        [TestMethod()]
        public async Task UserNotOwnerOfDeck()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);
            var card = await CardHelper.CreateAsync(db, user);
            await DeckHelper.AddCardAsync(db, deck, card.Id);
            var otherUser = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new RemoveCardsFromDeck(dbContext).RunAsync(new RemoveCardsFromDeck.Request(otherUser, deck, card.Id.AsArray())));
        }
        [TestMethod()]
        public async Task DoesNotThrowWhenCardDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);

            using var dbContext = new MemCheckDbContext(db);
            await new RemoveCardsFromDeck(dbContext).RunAsync(new RemoveCardsFromDeck.Request(user, deck, Guid.NewGuid().AsArray()));
        }
        [TestMethod()]
        public async Task DoesNotThrowWhenOneCardNotInTheDeck()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);
            var card = await CardHelper.CreateAsync(db, user);

            using var dbContext = new MemCheckDbContext(db);
            await new RemoveCardsFromDeck(dbContext).RunAsync(new RemoveCardsFromDeck.Request(user, deck, card.Id.AsArray()));
        }
        [TestMethod()]
        public async Task DoesNotThrowWhenCardNotInTheDeck()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);
            var card1 = await CardHelper.CreateAsync(db, user);
            var card2 = await CardHelper.CreateAsync(db, user);
            var card3 = await CardHelper.CreateAsync(db, user);
            await DeckHelper.AddCardAsync(db, deck, card1.Id);
            await DeckHelper.AddCardAsync(db, deck, card3.Id);

            using var dbContext = new MemCheckDbContext(db);
            await new RemoveCardsFromDeck(dbContext).RunAsync(new RemoveCardsFromDeck.Request(user, deck, new[] { card1.Id, card2.Id }));
        }
        [TestMethod()]
        public async Task OnlyCardInTheDeck()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);
            var card = await CardHelper.CreateAsync(db, user);
            await DeckHelper.AddCardAsync(db, deck, card.Id);

            using (var dbContext = new MemCheckDbContext(db))
                await new RemoveCardsFromDeck(dbContext).RunAsync(new RemoveCardsFromDeck.Request(user, deck, card.Id.AsArray()));

            await DeckHelper.CheckDeckDoesNotContainCard(db, deck, card.Id);
        }
        [TestMethod()]
        public async Task Complex()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);
            var card1 = await CardHelper.CreateAsync(db, user);
            await DeckHelper.AddCardAsync(db, deck, card1.Id);
            var card2 = await CardHelper.CreateAsync(db, user);
            await DeckHelper.AddCardAsync(db, deck, card2.Id);
            var card3 = await CardHelper.CreateAsync(db, user);
            await DeckHelper.AddCardAsync(db, deck, card3.Id);

            using (var dbContext = new MemCheckDbContext(db))
                await new RemoveCardsFromDeck(dbContext).RunAsync(new RemoveCardsFromDeck.Request(user, deck, new[] { card1.Id, card3.Id }));

            await DeckHelper.CheckDeckDoesNotContainCard(db, deck, card1.Id);
            await DeckHelper.CheckDeckDoesNotContainCard(db, deck, card3.Id);
            await DeckHelper.CheckDeckContainsCards(db, deck, card2.Id);
        }
    }
}
