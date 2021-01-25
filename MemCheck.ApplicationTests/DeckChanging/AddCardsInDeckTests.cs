using MemCheck.Application.QueryValidation;
using MemCheck.Application.Tests.Helpers;
using MemCheck.Basics;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.DeckChanging
{
    [TestClass()]
    public class AddCardsInDeckTests
    {
        [TestMethod()]
        public async Task UserNotLoggedIn()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new AddCardsInDeck(dbContext).RunAsync(new AddCardsInDeck.Request(Guid.Empty, deck, Array.Empty<Guid>())));
        }
        [TestMethod()]
        public async Task UserDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new AddCardsInDeck(dbContext).RunAsync(new AddCardsInDeck.Request(Guid.NewGuid(), deck, Array.Empty<Guid>())));
        }
        [TestMethod()]
        public async Task DeckDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new AddCardsInDeck(dbContext).RunAsync(new AddCardsInDeck.Request(user, Guid.NewGuid(), Array.Empty<Guid>())));
        }
        [TestMethod()]
        public async Task UserNotOwnerOfDeck()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deckOwner = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, deckOwner);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new AddCardsInDeck(dbContext).RunAsync(new AddCardsInDeck.Request(user, deck, Array.Empty<Guid>())));
        }
        [TestMethod()]
        public async Task CardDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<DbUpdateException>(async () => await new AddCardsInDeck(dbContext).RunAsync(new AddCardsInDeck.Request(user, deck, Guid.NewGuid().ToEnumerable())));
        }
        [TestMethod()]
        public async Task ACardDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);
            var card = await CardHelper.CreateAsync(db, user);
            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<DbUpdateException>(async () => await new AddCardsInDeck(dbContext).RunAsync(new AddCardsInDeck.Request(user, deck, new[] { card.Id, Guid.NewGuid() })));
        }
        [TestMethod()]
        public async Task CardAlreadyInDeck()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);
            var card = await CardHelper.CreateAsync(db, user);
            using (var dbContext = new MemCheckDbContext(db))
                await new AddCardsInDeck(dbContext).RunAsync(new AddCardsInDeck.Request(user, deck, card.Id.ToEnumerable()));
            using (var dbContext = new MemCheckDbContext(db))
                await new AddCardsInDeck(dbContext).RunAsync(new AddCardsInDeck.Request(user, deck, card.Id.ToEnumerable()));
            await DeckHelper.CheckDeckContainsCards(db, deck, card.Id);
        }
        [TestMethod()]
        public async Task ACardIsAlreadyInTheDeck()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);
            var card1 = await CardHelper.CreateAsync(db, user);
            var card2 = await CardHelper.CreateAsync(db, user);
            using (var dbContext = new MemCheckDbContext(db))
                await new AddCardsInDeck(dbContext).RunAsync(new AddCardsInDeck.Request(user, deck, card1.Id.ToEnumerable()));
            using (var dbContext = new MemCheckDbContext(db))
                await new AddCardsInDeck(dbContext).RunAsync(new AddCardsInDeck.Request(user, deck, new[] { card2.Id, card1.Id }));
            await DeckHelper.CheckDeckContainsCards(db, deck, new[] { card2.Id, card1.Id });
        }
        [TestMethod()]
        public async Task UserNotAllowedToViewCard()
        {
            var db = DbHelper.GetEmptyTestDB();
            var cardCreator = await UserHelper.CreateInDbAsync(db);
            var card = await CardHelper.CreateAsync(db, cardCreator, userWithViewIds: cardCreator.ToEnumerable());
            var deckOwner = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, deckOwner);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<ApplicationException>(async () => await new AddCardsInDeck(dbContext).RunAsync(new AddCardsInDeck.Request(deckOwner, deck, card.Id.ToEnumerable())));
        }
        [TestMethod()]
        public async Task UserNotAllowedToViewACard()
        {
            var db = DbHelper.GetEmptyTestDB();
            var cardCreator = await UserHelper.CreateInDbAsync(db);
            var cardNotAllowed = await CardHelper.CreateAsync(db, cardCreator, userWithViewIds: cardCreator.ToEnumerable());
            var deckOwner = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, deckOwner);
            var publicCard = await CardHelper.CreateAsync(db, cardCreator);

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<ApplicationException>(async () => await new AddCardsInDeck(dbContext).RunAsync(new AddCardsInDeck.Request(deckOwner, deck, new[] { publicCard.Id, cardNotAllowed.Id })));
        }
        [TestMethod()]
        public async Task Success()
        {
            var db = DbHelper.GetEmptyTestDB();
            var cardCreator = await UserHelper.CreateInDbAsync(db);
            var deckOwner = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, deckOwner);

            var card1 = await CardHelper.CreateAsync(db, cardCreator, userWithViewIds: new Guid[] { cardCreator, deckOwner });
            var card2 = await CardHelper.CreateAsync(db, deckOwner, userWithViewIds: new Guid[] { deckOwner });
            var card3 = await CardHelper.CreateAsync(db, cardCreator);

            using (var dbContext = new MemCheckDbContext(db))
                await new AddCardsInDeck(dbContext).RunAsync(new AddCardsInDeck.Request(deckOwner, deck, new[] { card1.Id, card2.Id, card3.Id }));

            await DeckHelper.CheckDeckContainsCards(db, deck, card1.Id, card2.Id, card3.Id);
        }
    }
}



