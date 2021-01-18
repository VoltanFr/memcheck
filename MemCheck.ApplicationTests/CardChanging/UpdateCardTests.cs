using MemCheck.Application.QueryValidation;
using MemCheck.Application.Tests.Helpers;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.CardChanging
{
    [TestClass()]
    public class UpdateCardTests
    {
        [TestMethod()]
        public async Task UserNotLoggedIn()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, user, language: languageId);

            using var dbContext = new MemCheckDbContext(db);
            var request = UpdateCardHelper.RequestForFrontSideChange(card, StringHelper.RandomString(), Guid.Empty);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new UpdateCard(dbContext).RunAsync(request, new TestLocalizer()));
        }
        [TestMethod()]
        public async Task UserDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, user, language: languageId);

            using var dbContext = new MemCheckDbContext(db);
            var r = UpdateCardHelper.RequestForFrontSideChange(card, StringHelper.RandomString(), Guid.NewGuid());
            await Assert.ThrowsExceptionAsync<ApplicationException>(async () => await new UpdateCard(dbContext).RunAsync(r, new TestLocalizer()));
        }
        [TestMethod()]
        public async Task CardDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            var r = new UpdateCard.Request(Guid.NewGuid(), user, StringHelper.RandomString(), Array.Empty<Guid>(), StringHelper.RandomString(), Array.Empty<Guid>(), StringHelper.RandomString(), Array.Empty<Guid>(), Guid.NewGuid(), Array.Empty<Guid>(), Array.Empty<Guid>(), StringHelper.RandomString());
            await Assert.ThrowsExceptionAsync<ApplicationException>(async () => await new UpdateCard(dbContext).RunAsync(r, new TestLocalizer()));
        }
        [TestMethod()]
        public async Task UserNotAllowedToViewCard()
        {
            var db = DbHelper.GetEmptyTestDB();
            var cardCreator = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, cardCreator, language: languageId, userWithViewIds: new[] { cardCreator });
            var otherUser = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            var r = UpdateCardHelper.RequestForFrontSideChange(card, StringHelper.RandomString(), otherUser);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new UpdateCard(dbContext).RunAsync(r, new TestLocalizer()));
        }
        [TestMethod()]
        public async Task PublicCard()
        {
            var db = DbHelper.GetEmptyTestDB();
            var cardCreator = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, cardCreator, language: languageId, userWithViewIds: Array.Empty<Guid>());
            var otherUser = await UserHelper.CreateInDbAsync(db);
            var newFrontSide = StringHelper.RandomString();

            using (var dbContext = new MemCheckDbContext(db))
            {
                var r = UpdateCardHelper.RequestForFrontSideChange(card, newFrontSide, otherUser);
                await new UpdateCard(dbContext).RunAsync(r, new TestLocalizer());
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var cardFromDb = dbContext.Cards.Single(c => c.Id == card.Id);
                Assert.AreEqual(newFrontSide, cardFromDb.FrontSide);
            }
        }
        [TestMethod()]
        public async Task UserNotInNewVisibilityList()
        {
            var db = DbHelper.GetEmptyTestDB();
            var cardCreator = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, cardCreator, language: languageId, userWithViewIds: Array.Empty<Guid>());
            var newVersionCreator = await UserHelper.CreateInDbAsync(db);

            var r = UpdateCardHelper.RequestForVisibilityChange(card, new[] { cardCreator }) with { VersionCreatorId = newVersionCreator };

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new UpdateCard(dbContext).RunAsync(r, new TestLocalizer()));
        }
        [TestMethod()]
        public async Task UserInVisibilityList()
        {
            var db = DbHelper.GetEmptyTestDB();
            var cardCreator = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var otherUser = await UserHelper.CreateInDbAsync(db);
            var card = await CardHelper.CreateAsync(db, cardCreator, language: languageId, userWithViewIds: new[] { cardCreator, otherUser });
            var newFrontSide = StringHelper.RandomString();

            using (var dbContext = new MemCheckDbContext(db))
            {
                var r = UpdateCardHelper.RequestForFrontSideChange(card, newFrontSide, otherUser);
                await new UpdateCard(dbContext).RunAsync(r, new TestLocalizer());
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var cardFromDb = dbContext.Cards.Single(c => c.Id == card.Id);
                Assert.AreEqual(newFrontSide, cardFromDb.FrontSide);
            }
        }
        [TestMethod()]
        public async Task UpdateAllFields()
        {
            var db = DbHelper.GetEmptyTestDB();
            var cardCreator = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var originalCard = await CardHelper.CreateAsync(db, cardCreator, language: languageId, userWithViewIds: Array.Empty<Guid>());

            var newVersionCreator = await UserHelper.CreateInDbAsync(db);
            var frontSide = StringHelper.RandomString();
            var backSide = StringHelper.RandomString();
            var additionalInfo = StringHelper.RandomString();
            var versionDescription = StringHelper.RandomString();
            var newLanguageId = await CardLanguagHelper.CreateAsync(db);
            var imageOnFrontSideId = await ImageHelper.CreateAsync(db, cardCreator);
            var imageOnBackSide1Id = await ImageHelper.CreateAsync(db, cardCreator);
            var imageOnBackSide2Id = await ImageHelper.CreateAsync(db, cardCreator);
            var imageOnAdditionalInfoId = await ImageHelper.CreateAsync(db, cardCreator);
            var tagId = await TagHelper.CreateAsync(db);

            using (var dbContext = new MemCheckDbContext(db))
            {
                var request = new UpdateCard.Request(
                    originalCard.Id,
                    newVersionCreator,
                    frontSide,
                    new Guid[] { imageOnFrontSideId },
                    backSide,
                    new Guid[] { imageOnBackSide1Id, imageOnBackSide2Id },
                    additionalInfo,
                    new Guid[] { imageOnAdditionalInfoId },
                    languageId,
                    new Guid[] { tagId },
                    new Guid[] { cardCreator, newVersionCreator },
                    versionDescription);
                await new UpdateCard(dbContext).RunAsync(request, new TestLocalizer());
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var updatedCard = dbContext.Cards
                    .Include(c => c.VersionCreator)
                    .Include(c => c.CardLanguage)
                    .Include(c => c.UsersWithView)
                    .Include(c => c.Images)
                    .Include(c => c.TagsInCards)
                    .Single(c => c.Id == originalCard.Id);
                Assert.AreEqual(newVersionCreator, updatedCard.VersionCreator.Id);
                Assert.AreEqual(frontSide, updatedCard.FrontSide);
                Assert.AreEqual(backSide, updatedCard.BackSide);
                Assert.AreEqual(additionalInfo, updatedCard.AdditionalInfo);
                Assert.AreEqual(versionDescription, updatedCard.VersionDescription);
                Assert.AreEqual(languageId, updatedCard.CardLanguage.Id);
                Assert.AreEqual(ImageInCard.FrontSide, updatedCard.Images.Single(i => i.ImageId == imageOnFrontSideId).CardSide);
                Assert.AreEqual(ImageInCard.BackSide, updatedCard.Images.Single(i => i.ImageId == imageOnBackSide1Id).CardSide);
                Assert.AreEqual(ImageInCard.BackSide, updatedCard.Images.Single(i => i.ImageId == imageOnBackSide2Id).CardSide);
                Assert.AreEqual(ImageInCard.AdditionalInfo, updatedCard.Images.Single(i => i.ImageId == imageOnAdditionalInfoId).CardSide);
                Assert.IsTrue(updatedCard.TagsInCards.Any(t => t.TagId == tagId));
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                await CardVisibilityHelper.CheckUserIsAllowedToViewCardAsync(dbContext, cardCreator, originalCard.Id);
                await CardVisibilityHelper.CheckUserIsAllowedToViewCardAsync(dbContext, newVersionCreator, originalCard.Id);
            }
        }
        [TestMethod()]
        public async Task UpdateNothing()
        {
            var db = DbHelper.GetEmptyTestDB();
            var cardCreator = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, cardCreator, language: languageId, userWithViewIds: Array.Empty<Guid>());

            using var dbContext = new MemCheckDbContext(db);
            var request = new UpdateCard.Request(
                card.Id,
                card.VersionCreator.Id,
                card.FrontSide,
                card.Images.Where(i => i.CardSide == ImageInCard.FrontSide).Select(i => i.ImageId),
                card.BackSide,
                card.Images.Where(i => i.CardSide == ImageInCard.BackSide).Select(i => i.ImageId),
                card.AdditionalInfo,
                card.Images.Where(i => i.CardSide == ImageInCard.AdditionalInfo).Select(i => i.ImageId),
                card.CardLanguage.Id,
                card.TagsInCards.Select(t => t.TagId),
                card.UsersWithView.Select(uwv => uwv.UserId),
                StringHelper.RandomString());
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateCard(dbContext).RunAsync(request, new TestLocalizer()));
        }
        [TestMethod()]
        public async Task UserSubscribingToCardOnEdit()
        {
            var db = DbHelper.GetEmptyTestDB();

            var cardCreator = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, cardCreator, language: languageId);

            var newVersionCreator = await UserHelper.CreateInDbAsync(db, subscribeToCardOnEdit: true);

            using (var dbContext = new MemCheckDbContext(db))
            {
                var r = UpdateCardHelper.RequestForFrontSideChange(card, StringHelper.RandomString(), newVersionCreator);
                await new UpdateCard(dbContext).RunAsync(r, new TestLocalizer());
            }

            Assert.IsTrue(await CardSubscriptionHelper.UserIsSubscribedToCardAsync(db, newVersionCreator, card.Id));
        }
        [TestMethod()]
        public async Task UserNotSubscribingToCardOnEdit()
        {
            var db = DbHelper.GetEmptyTestDB();

            var cardCreator = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, cardCreator, language: languageId);

            var newVersionCreator = await UserHelper.CreateInDbAsync(db, subscribeToCardOnEdit: false);

            using (var dbContext = new MemCheckDbContext(db))
            {
                var r = UpdateCardHelper.RequestForFrontSideChange(card, StringHelper.RandomString(), newVersionCreator);
                await new UpdateCard(dbContext).RunAsync(r, new TestLocalizer());
            }

            Assert.IsFalse(await CardSubscriptionHelper.UserIsSubscribedToCardAsync(db, newVersionCreator, card.Id));
        }
        [TestMethod()]
        public async Task ReduceVisibility_OnlyUserWithView_NoOtherUserHasInDeck_OnlyAuthor()
        {
            var db = DbHelper.GetEmptyTestDB();
            var cardCreator = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, cardCreator, language: languageId, userWithViewIds: Array.Empty<Guid>());
            var deck = await DeckHelper.CreateAsync(db, cardCreator);
            await DeckHelper.AddCardAsync(db, cardCreator, deck, card.Id, 0);

            var otherUser = await UserHelper.CreateInDbAsync(db);

            using (var dbContext = new MemCheckDbContext(db))
            {
                await CardVisibilityHelper.CheckUserIsAllowedToViewCardAsync(dbContext, cardCreator, card.Id);
                await CardVisibilityHelper.CheckUserIsAllowedToViewCardAsync(dbContext, otherUser, card.Id);
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var r = UpdateCardHelper.RequestForVisibilityChange(card, new[] { cardCreator });
                await new UpdateCard(dbContext).RunAsync(r, new TestLocalizer());
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                await CardVisibilityHelper.CheckUserIsAllowedToViewCardAsync(dbContext, cardCreator, card.Id);
                await Assert.ThrowsExceptionAsync<ApplicationException>(async () => await CardVisibilityHelper.CheckUserIsAllowedToViewCardAsync(dbContext, otherUser, card.Id));
            }
        }
        [TestMethod()]
        public async Task ReduceVisibility_OtherUserHasView_NoUserHasInDeck_OnlyAuthor()
        {
            var db = DbHelper.GetEmptyTestDB();
            var cardCreator = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var otherUser = await UserHelper.CreateInDbAsync(db);
            var card = await CardHelper.CreateAsync(db, cardCreator, language: languageId, userWithViewIds: new[] { cardCreator, otherUser });

            using (var dbContext = new MemCheckDbContext(db))
            {
                await CardVisibilityHelper.CheckUserIsAllowedToViewCardAsync(dbContext, cardCreator, card.Id);
                await CardVisibilityHelper.CheckUserIsAllowedToViewCardAsync(dbContext, otherUser, card.Id);
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var r = UpdateCardHelper.RequestForVisibilityChange(card, new[] { cardCreator });
                await new UpdateCard(dbContext).RunAsync(r, new TestLocalizer());
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                await CardVisibilityHelper.CheckUserIsAllowedToViewCardAsync(dbContext, cardCreator, card.Id);
                await Assert.ThrowsExceptionAsync<ApplicationException>(async () => await CardVisibilityHelper.CheckUserIsAllowedToViewCardAsync(dbContext, otherUser, card.Id));
            }
        }
        [TestMethod()]
        public async Task ReduceVisibility_OtherUserHasInDeck_OnlyAuthor()
        {
            var db = DbHelper.GetEmptyTestDB();
            var cardCreatorName = StringHelper.RandomString();
            var cardCreator = await UserHelper.CreateInDbAsync(db, userName: cardCreatorName);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, cardCreator, language: languageId, userWithViewIds: Array.Empty<Guid>());

            var otherUserName = StringHelper.RandomString();
            var otherUser = await UserHelper.CreateInDbAsync(db, userName: otherUserName);
            var otherUserDeck = await DeckHelper.CreateAsync(db, otherUser);
            await DeckHelper.AddCardAsync(db, otherUser, otherUserDeck, card.Id, 0);

            using (var dbContext = new MemCheckDbContext(db))
            {
                var r = UpdateCardHelper.RequestForVisibilityChange(card, new[] { cardCreator });
                var e = await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateCard(dbContext).RunAsync(r, new TestLocalizer()));
                Assert.IsTrue(e.Message.Contains(otherUserName));
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var r = UpdateCardHelper.RequestForVisibilityChange(card, new[] { otherUser }, otherUser);
                var e = await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateCard(dbContext).RunAsync(r, new TestLocalizer()));
                Assert.IsTrue(e.Message.Contains(cardCreatorName));
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var r = UpdateCardHelper.RequestForVisibilityChange(card, new[] { cardCreator, otherUser });
                await new UpdateCard(dbContext).RunAsync(r, new TestLocalizer());
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                await CardVisibilityHelper.CheckUserIsAllowedToViewCardAsync(dbContext, cardCreator, card.Id);
                await CardVisibilityHelper.CheckUserIsAllowedToViewCardAsync(dbContext, otherUser, card.Id);
            }
        }
        [TestMethod()]
        public async Task ReduceVisibility_NoUserHasInDeck_OtherAuthor()
        {
            var db = DbHelper.GetEmptyTestDB();
            var cardCreatorName = StringHelper.RandomString();
            var cardCreator = await UserHelper.CreateInDbAsync(db, userName: cardCreatorName);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, cardCreator, language: languageId, userWithViewIds: Array.Empty<Guid>());

            var newVersionCreatorName = StringHelper.RandomString();
            var newVersionCreator = await UserHelper.CreateInDbAsync(db, userName: newVersionCreatorName);
            using (var dbContext = new MemCheckDbContext(db))
            {
                var r = UpdateCardHelper.RequestForFrontSideChange(card, StringHelper.RandomString(), newVersionCreator);
                await new UpdateCard(dbContext).RunAsync(r, new TestLocalizer());
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var r = UpdateCardHelper.RequestForVisibilityChange(card, new[] { cardCreator }, cardCreator);
                var e = await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateCard(dbContext).RunAsync(r, new TestLocalizer()));
                Assert.IsTrue(e.Message.Contains(newVersionCreatorName));
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var r = UpdateCardHelper.RequestForVisibilityChange(card, new[] { newVersionCreator }, newVersionCreator);
                var e = await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateCard(dbContext).RunAsync(r, new TestLocalizer()));
                Assert.IsTrue(e.Message.Contains(cardCreatorName));
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var r = UpdateCardHelper.RequestForVisibilityChange(card, new[] { cardCreator, newVersionCreator });
                await new UpdateCard(dbContext).RunAsync(r, new TestLocalizer());
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                await CardVisibilityHelper.CheckUserIsAllowedToViewCardAsync(dbContext, cardCreator, card.Id);
                await CardVisibilityHelper.CheckUserIsAllowedToViewCardAsync(dbContext, newVersionCreator, card.Id);
            }
        }
        [TestMethod()]
        public async Task ReduceVisibility_OtherUserHasInDeck_OtherAuthor()
        {
            var db = DbHelper.GetEmptyTestDB();
            var cardCreator = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, cardCreator, language: languageId, userWithViewIds: Array.Empty<Guid>());

            var userWithCardInDeck = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, userWithCardInDeck);
            await DeckHelper.AddCardAsync(db, userWithCardInDeck, deck, card.Id, 0);

            var otherUser = await UserHelper.CreateInDbAsync(db);
            using (var dbContext = new MemCheckDbContext(db))
            {
                var r = UpdateCardHelper.RequestForFrontSideChange(card, StringHelper.RandomString(), otherUser);
                await new UpdateCard(dbContext).RunAsync(r, new TestLocalizer());
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var r = UpdateCardHelper.RequestForVisibilityChange(card, new[] { cardCreator });
                await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateCard(dbContext).RunAsync(r, new TestLocalizer()));
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var r = UpdateCardHelper.RequestForVisibilityChange(card, new[] { otherUser }, otherUser);
                await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateCard(dbContext).RunAsync(r, new TestLocalizer()));
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var r = UpdateCardHelper.RequestForVisibilityChange(card, new[] { userWithCardInDeck }, userWithCardInDeck);
                await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateCard(dbContext).RunAsync(r, new TestLocalizer()));
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var r = UpdateCardHelper.RequestForVisibilityChange(card, new[] { cardCreator, otherUser, userWithCardInDeck });
                await new UpdateCard(dbContext).RunAsync(r, new TestLocalizer());
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                await CardVisibilityHelper.CheckUserIsAllowedToViewCardAsync(dbContext, cardCreator, card.Id);
                await CardVisibilityHelper.CheckUserIsAllowedToViewCardAsync(dbContext, otherUser, card.Id);
                await CardVisibilityHelper.CheckUserIsAllowedToViewCardAsync(dbContext, userWithCardInDeck, card.Id);
            }
        }
    }
}
