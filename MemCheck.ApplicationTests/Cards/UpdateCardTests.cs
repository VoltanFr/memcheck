using MemCheck.Application.Helpers;
using MemCheck.Application.QueryValidation;
using MemCheck.Application.Ratings;
using MemCheck.Basics;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards
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
            var request = UpdateCardHelper.RequestForFrontSideChange(card, RandomHelper.String(), Guid.Empty);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new UpdateCard(dbContext.AsCallContext()).RunAsync(request));
        }
        [TestMethod()]
        public async Task UserDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var frontSide = RandomHelper.String();
            var card = await CardHelper.CreateAsync(db, user, language: languageId, frontSide: frontSide);

            using var dbContext = new MemCheckDbContext(db);
            var r = UpdateCardHelper.RequestForFrontSideChange(card, RandomHelper.String(), Guid.NewGuid());
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new UpdateCard(dbContext.AsCallContext()).RunAsync(r));
            await CardHelper.AssertCardHasFrontSide(db, card.Id, frontSide);
        }
        [TestMethod()]
        public async Task CardDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            var r = new UpdateCard.Request(Guid.NewGuid(), user, RandomHelper.String(), Array.Empty<Guid>(), RandomHelper.String(), Array.Empty<Guid>(), RandomHelper.String(), Array.Empty<Guid>(), RandomHelper.String(), Guid.NewGuid(), Array.Empty<Guid>(), Array.Empty<Guid>(), RandomHelper.String());
            await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await new UpdateCard(dbContext.AsCallContext()).RunAsync(r));
        }
        [TestMethod()]
        public async Task UserNotAllowedToViewCard()
        {
            var db = DbHelper.GetEmptyTestDB();
            var cardCreator = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var frontSide = RandomHelper.String();
            var card = await CardHelper.CreateAsync(db, cardCreator, language: languageId, userWithViewIds: cardCreator.AsArray(), frontSide: frontSide);
            var otherUser = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            var r = UpdateCardHelper.RequestForFrontSideChange(card, RandomHelper.String(), otherUser);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new UpdateCard(dbContext.AsCallContext()).RunAsync(r));
            await CardHelper.AssertCardHasFrontSide(db, card.Id, frontSide);
        }
        [TestMethod()]
        public async Task PublicCard()
        {
            var db = DbHelper.GetEmptyTestDB();
            var cardCreator = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, cardCreator, language: languageId, userWithViewIds: Array.Empty<Guid>());
            var otherUser = await UserHelper.CreateInDbAsync(db);
            var newFrontSide = RandomHelper.String();

            using (var dbContext = new MemCheckDbContext(db))
            {
                var r = UpdateCardHelper.RequestForFrontSideChange(card, newFrontSide, otherUser);
                await new UpdateCard(dbContext.AsCallContext()).RunAsync(r);
            }

            await CardHelper.AssertCardHasFrontSide(db, card.Id, newFrontSide);
        }
        [TestMethod()]
        public async Task UserNotInNewVisibilityList()
        {
            var db = DbHelper.GetEmptyTestDB();
            var cardCreator = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, cardCreator, language: languageId, userWithViewIds: Array.Empty<Guid>());
            var newVersionCreator = await UserHelper.CreateInDbAsync(db);

            var r = UpdateCardHelper.RequestForVisibilityChange(card, cardCreator.AsArray()) with { VersionCreatorId = newVersionCreator };

            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new UpdateCard(dbContext.AsCallContext()).RunAsync(r));
        }
        [TestMethod()]
        public async Task DescriptionTooShort()
        {
            var db = DbHelper.GetEmptyTestDB();
            var cardCreator = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, cardCreator, language: languageId, userWithViewIds: Array.Empty<Guid>());
            var request = UpdateCardHelper.RequestForFrontSideChange(card, RandomHelper.String(), versionDescription: RandomHelper.String(CardInputValidator.MinVersionDescriptionLength - 1));
            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateCard(dbContext.AsCallContext()).RunAsync(request));
        }
        [TestMethod()]
        public async Task DescriptionTooLong()
        {
            var db = DbHelper.GetEmptyTestDB();
            var cardCreator = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, cardCreator, language: languageId, userWithViewIds: Array.Empty<Guid>());
            var request = UpdateCardHelper.RequestForFrontSideChange(card, RandomHelper.String(), versionDescription: RandomHelper.String(CardInputValidator.MaxVersionDescriptionLength + 1));
            using var dbContext = new MemCheckDbContext(db);
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateCard(dbContext.AsCallContext()).RunAsync(request));
        }
        [TestMethod()]
        public async Task UserInVisibilityList()
        {
            var db = DbHelper.GetEmptyTestDB();
            var cardCreator = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var otherUser = await UserHelper.CreateInDbAsync(db);
            var card = await CardHelper.CreateAsync(db, cardCreator, language: languageId, userWithViewIds: new[] { cardCreator, otherUser });
            var newFrontSide = RandomHelper.String();

            using (var dbContext = new MemCheckDbContext(db))
            {
                var r = UpdateCardHelper.RequestForFrontSideChange(card, newFrontSide, otherUser);
                await new UpdateCard(dbContext.AsCallContext()).RunAsync(r);
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
            var frontSide = RandomHelper.String();
            var backSide = RandomHelper.String();
            var additionalInfo = RandomHelper.String();
            var references = RandomHelper.String();
            var versionDescription = RandomHelper.String();
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
                    references,
                    languageId,
                    new Guid[] { tagId },
                    new Guid[] { cardCreator, newVersionCreator },
                    versionDescription);
                await new UpdateCard(dbContext.AsCallContext()).RunAsync(request);
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
                Assert.AreEqual(references, updatedCard.References);
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
                CardVisibilityHelper.CheckUserIsAllowedToViewCard(dbContext, cardCreator, originalCard.Id);
                CardVisibilityHelper.CheckUserIsAllowedToViewCard(dbContext, newVersionCreator, originalCard.Id);
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
                card.References,
                card.CardLanguage.Id,
                card.TagsInCards.Select(t => t.TagId),
                card.UsersWithView.Select(uwv => uwv.UserId),
                RandomHelper.String());
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateCard(dbContext.AsCallContext()).RunAsync(request));
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
                var r = UpdateCardHelper.RequestForFrontSideChange(card, RandomHelper.String(), newVersionCreator);
                await new UpdateCard(dbContext.AsCallContext()).RunAsync(r);
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
                var r = UpdateCardHelper.RequestForFrontSideChange(card, RandomHelper.String(), newVersionCreator);
                await new UpdateCard(dbContext.AsCallContext()).RunAsync(r);
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
            await DeckHelper.AddCardAsync(db, deck, card.Id, 0);

            var otherUser = await UserHelper.CreateInDbAsync(db);

            using (var dbContext = new MemCheckDbContext(db))
            {
                CardVisibilityHelper.CheckUserIsAllowedToViewCard(dbContext, cardCreator, card.Id);
                CardVisibilityHelper.CheckUserIsAllowedToViewCard(dbContext, otherUser, card.Id);
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var r = UpdateCardHelper.RequestForVisibilityChange(card, cardCreator.AsArray());
                await new UpdateCard(dbContext.AsCallContext()).RunAsync(r);
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                CardVisibilityHelper.CheckUserIsAllowedToViewCard(dbContext, cardCreator, card.Id);
                Assert.ThrowsException<InvalidOperationException>(() => CardVisibilityHelper.CheckUserIsAllowedToViewCard(dbContext, otherUser, card.Id));
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
                CardVisibilityHelper.CheckUserIsAllowedToViewCard(dbContext, cardCreator, card.Id);
                CardVisibilityHelper.CheckUserIsAllowedToViewCard(dbContext, otherUser, card.Id);
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var r = UpdateCardHelper.RequestForVisibilityChange(card, cardCreator.AsArray());
                await new UpdateCard(dbContext.AsCallContext()).RunAsync(r);
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                CardVisibilityHelper.CheckUserIsAllowedToViewCard(dbContext, cardCreator, card.Id);
                Assert.ThrowsException<InvalidOperationException>(() => CardVisibilityHelper.CheckUserIsAllowedToViewCard(dbContext, otherUser, card.Id));
            }
        }
        [TestMethod()]
        public async Task ReduceVisibility_OtherUserHasInDeck_OnlyAuthor()
        {
            var db = DbHelper.GetEmptyTestDB();
            var cardCreatorName = RandomHelper.String();
            var cardCreator = await UserHelper.CreateInDbAsync(db, userName: cardCreatorName);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, cardCreator, language: languageId, userWithViewIds: Array.Empty<Guid>());

            var otherUserName = RandomHelper.String();
            var otherUser = await UserHelper.CreateInDbAsync(db, userName: otherUserName);
            var otherUserDeck = await DeckHelper.CreateAsync(db, otherUser);
            await DeckHelper.AddCardAsync(db, otherUserDeck, card.Id, 0);

            using (var dbContext = new MemCheckDbContext(db))
            {
                var r = UpdateCardHelper.RequestForVisibilityChange(card, cardCreator.AsArray());
                var e = await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateCard(dbContext.AsCallContext()).RunAsync(r));
                Assert.IsTrue(e.Message.Contains(otherUserName));
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var r = UpdateCardHelper.RequestForVisibilityChange(card, otherUser.AsArray(), otherUser);
                var e = await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateCard(dbContext.AsCallContext()).RunAsync(r));
                Assert.IsTrue(e.Message.Contains(cardCreatorName));
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var r = UpdateCardHelper.RequestForVisibilityChange(card, new[] { cardCreator, otherUser });
                await new UpdateCard(dbContext.AsCallContext()).RunAsync(r);
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                CardVisibilityHelper.CheckUserIsAllowedToViewCard(dbContext, cardCreator, card.Id);
                CardVisibilityHelper.CheckUserIsAllowedToViewCard(dbContext, otherUser, card.Id);
            }
        }
        [TestMethod()]
        public async Task ReduceVisibility_NoUserHasInDeck_OtherAuthor()
        {
            var db = DbHelper.GetEmptyTestDB();
            var cardCreatorName = RandomHelper.String();
            var cardCreator = await UserHelper.CreateInDbAsync(db, userName: cardCreatorName);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, cardCreator, language: languageId, userWithViewIds: Array.Empty<Guid>());

            var newVersionCreatorName = RandomHelper.String();
            var newVersionCreator = await UserHelper.CreateInDbAsync(db, userName: newVersionCreatorName);
            using (var dbContext = new MemCheckDbContext(db))
            {
                var r = UpdateCardHelper.RequestForFrontSideChange(card, RandomHelper.String(), newVersionCreator);
                await new UpdateCard(dbContext.AsCallContext()).RunAsync(r);
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var r = UpdateCardHelper.RequestForVisibilityChange(card, cardCreator.AsArray(), cardCreator);
                var e = await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateCard(dbContext.AsCallContext()).RunAsync(r));
                Assert.IsTrue(e.Message.Contains(newVersionCreatorName));
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var r = UpdateCardHelper.RequestForVisibilityChange(card, new[] { newVersionCreator }, newVersionCreator);
                var e = await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateCard(dbContext.AsCallContext()).RunAsync(r));
                Assert.IsTrue(e.Message.Contains(cardCreatorName));
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var r = UpdateCardHelper.RequestForVisibilityChange(card, new[] { cardCreator, newVersionCreator });
                await new UpdateCard(dbContext.AsCallContext()).RunAsync(r);
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                CardVisibilityHelper.CheckUserIsAllowedToViewCard(dbContext, cardCreator, card.Id);
                CardVisibilityHelper.CheckUserIsAllowedToViewCard(dbContext, newVersionCreator, card.Id);
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
            await DeckHelper.AddCardAsync(db, deck, card.Id, 0);

            var otherUser = await UserHelper.CreateInDbAsync(db);
            using (var dbContext = new MemCheckDbContext(db))
            {
                var r = UpdateCardHelper.RequestForFrontSideChange(card, RandomHelper.String(), otherUser);
                await new UpdateCard(dbContext.AsCallContext()).RunAsync(r);
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var r = UpdateCardHelper.RequestForVisibilityChange(card, cardCreator.AsArray());
                await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateCard(dbContext.AsCallContext()).RunAsync(r));
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var r = UpdateCardHelper.RequestForVisibilityChange(card, otherUser.AsArray(), otherUser);
                await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateCard(dbContext.AsCallContext()).RunAsync(r));
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var r = UpdateCardHelper.RequestForVisibilityChange(card, new[] { userWithCardInDeck }, userWithCardInDeck);
                await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateCard(dbContext.AsCallContext()).RunAsync(r));
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var r = UpdateCardHelper.RequestForVisibilityChange(card, new[] { cardCreator, otherUser, userWithCardInDeck });
                await new UpdateCard(dbContext.AsCallContext()).RunAsync(r);
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                CardVisibilityHelper.CheckUserIsAllowedToViewCard(dbContext, cardCreator, card.Id);
                CardVisibilityHelper.CheckUserIsAllowedToViewCard(dbContext, otherUser, card.Id);
                CardVisibilityHelper.CheckUserIsAllowedToViewCard(dbContext, userWithCardInDeck, card.Id);
            }
        }
        [TestMethod()]
        public async Task UpdateDoesNotAlterRatings()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var card = await CardHelper.CreateAsync(db, user, language: languageId);
            var rating = RandomHelper.Rating();

            using (var dbContext = new MemCheckDbContext(db))
                await new SetCardRating(dbContext.AsCallContext()).RunAsync(new SetCardRating.Request(user, card.Id, rating));

            using (var dbContext = new MemCheckDbContext(db))
                await new UpdateCard(dbContext.AsCallContext()).RunAsync(UpdateCardHelper.RequestForFrontSideChange(card, RandomHelper.String()));

            using (var dbContext = new MemCheckDbContext(db))
            {
                var loaded = dbContext.Cards.Single();
                Assert.AreEqual(1, loaded.RatingCount);
                Assert.AreEqual(rating, loaded.AverageRating);
            }
        }
        [TestMethod()]
        public async Task ReferencesTooLong()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var creatorId = await UserHelper.CreateInDbAsync(testDB);
            var originalCard = await CardHelper.CreateAsync(testDB, creatorId);

            var request = UpdateCardHelper.RequestForReferencesChange(originalCard, RandomHelper.String(CardInputValidator.MaxReferencesLength + 1));

            using var dbContext = new MemCheckDbContext(testDB);
            var exception = await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new UpdateCard(dbContext.AsCallContext()).RunAsync(request));
            StringAssert.Contains(exception.Message, CardInputValidator.MinReferencesLength.ToString());
            StringAssert.Contains(exception.Message, CardInputValidator.MaxReferencesLength.ToString());
            StringAssert.Contains(exception.Message, (CardInputValidator.MaxReferencesLength + 1).ToString());
        }
        [TestMethod()]
        public async Task UpdateFrontSideWithValueNotTrimmed()
        {
            var db = DbHelper.GetEmptyTestDB();
            var cardCreator = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var originalCard = await CardHelper.CreateAsync(db, cardCreator, language: languageId);

            var newFrontSide = RandomHelper.String();

            using (var dbContext = new MemCheckDbContext(db))
            {
                var request = new UpdateCard.Request(
                    originalCard.Id,
                    cardCreator,
                    newFrontSide + ' ',
                    Array.Empty<Guid>(),
                    originalCard.BackSide,
                    Array.Empty<Guid>(),
                    originalCard.AdditionalInfo,
                    Array.Empty<Guid>(),
                    originalCard.References,
                    languageId,
                    Array.Empty<Guid>(),
                    Array.Empty<Guid>(),
                    RandomHelper.String());
                await new UpdateCard(dbContext.AsCallContext()).RunAsync(request);
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var updatedCard = dbContext.Cards
                    .Include(c => c.VersionCreator)
                    .Include(c => c.CardLanguage)
                    .Include(c => c.UsersWithView)
                    .Include(c => c.Images)
                    .Include(c => c.TagsInCards)
                    .Single();
                Assert.AreEqual(cardCreator, updatedCard.VersionCreator.Id);
                Assert.AreEqual(newFrontSide, updatedCard.FrontSide);
                Assert.AreEqual(originalCard.BackSide, updatedCard.BackSide);
                Assert.AreEqual(originalCard.AdditionalInfo, updatedCard.AdditionalInfo);
                Assert.AreEqual(originalCard.References, updatedCard.References);
                Assert.AreEqual(languageId, updatedCard.CardLanguage.Id);
                Assert.IsFalse(updatedCard.Images.Any());
                Assert.IsFalse(updatedCard.TagsInCards.Any());
                Assert.IsFalse(updatedCard.UsersWithView.Any());
            }
        }
        [TestMethod()]
        public async Task UpdateReferencesWithValueNotTrimmed()
        {
            var db = DbHelper.GetEmptyTestDB();
            var cardCreator = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var originalCard = await CardHelper.CreateAsync(db, cardCreator, language: languageId);

            var newReferences = RandomHelper.String();

            using (var dbContext = new MemCheckDbContext(db))
            {
                var request = new UpdateCard.Request(
                    originalCard.Id,
                    cardCreator,
                    originalCard.FrontSide,
                    Array.Empty<Guid>(),
                    originalCard.BackSide,
                    Array.Empty<Guid>(),
                    originalCard.AdditionalInfo,
                    Array.Empty<Guid>(),
                    " " + newReferences,
                    languageId,
                    Array.Empty<Guid>(),
                    Array.Empty<Guid>(),
                    RandomHelper.String());
                await new UpdateCard(dbContext.AsCallContext()).RunAsync(request);
            }

            using (var dbContext = new MemCheckDbContext(db))
            {
                var updatedCard = dbContext.Cards
                    .Include(c => c.VersionCreator)
                    .Include(c => c.CardLanguage)
                    .Include(c => c.UsersWithView)
                    .Include(c => c.Images)
                    .Include(c => c.TagsInCards)
                    .Single();
                Assert.AreEqual(cardCreator, updatedCard.VersionCreator.Id);
                Assert.AreEqual(originalCard.FrontSide, updatedCard.FrontSide);
                Assert.AreEqual(originalCard.BackSide, updatedCard.BackSide);
                Assert.AreEqual(originalCard.AdditionalInfo, updatedCard.AdditionalInfo);
                Assert.AreEqual(newReferences, updatedCard.References);
                Assert.AreEqual(languageId, updatedCard.CardLanguage.Id);
                Assert.IsFalse(updatedCard.Images.Any());
                Assert.IsFalse(updatedCard.TagsInCards.Any());
                Assert.IsFalse(updatedCard.UsersWithView.Any());
            }
        }
        [TestMethod()]
        public async Task AddingPersoTagToPublicCardMustFail()
        {
            var db = DbHelper.GetEmptyTestDB();
            var creatorId = await UserHelper.CreateInDbAsync(db);
            var languageId = await CardLanguagHelper.CreateAsync(db);
            var cardId = await CardHelper.CreateIdAsync(db, creatorId, language: languageId, userWithViewIds: Array.Empty<Guid>());

            var persoTagId = await TagHelper.CreateAsync(db, name: Tag.Perso);

            var request = new UpdateCard.Request(
                cardId,
                creatorId,
                RandomHelper.String(),
                Array.Empty<Guid>(),
                RandomHelper.String(),
                Array.Empty<Guid>(),
                RandomHelper.String(),
                Array.Empty<Guid>(),
                RandomHelper.String(),
                languageId,
                persoTagId.AsArray(),
                Array.Empty<Guid>(),
                RandomHelper.String());

            var errorMesg = RandomHelper.String();
            var localizer = new TestLocalizer("PersoTagAllowedOnlyOnPrivateCards".PairedWith(errorMesg));
            using var dbContext = new MemCheckDbContext(db);
            var exception = await Assert.ThrowsExceptionAsync<PersoTagAllowedOnlyOnPrivateCardsException>(async () => await new UpdateCard(dbContext.AsCallContext(localizer)).RunAsync(request));
            Assert.AreEqual(errorMesg, exception.Message);
        }
        [TestMethod()]
        public async Task AddingPersoTagToCardVisibleToOtherMustFail()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var creatorId = await UserHelper.CreateInDbAsync(testDB);
            var otherUserId = await UserHelper.CreateInDbAsync(testDB);
            var languageId = await CardLanguagHelper.CreateAsync(testDB);
            var cardId = await CardHelper.CreateIdAsync(testDB, creatorId, language: languageId, userWithViewIds: new[] { creatorId, otherUserId });

            var persoTagId = await TagHelper.CreateAsync(testDB, name: Tag.Perso);

            var request = new UpdateCard.Request(
                cardId,
                creatorId,
                RandomHelper.String(),
                Array.Empty<Guid>(),
                RandomHelper.String(),
                Array.Empty<Guid>(),
                RandomHelper.String(),
                Array.Empty<Guid>(),
                RandomHelper.String(),
                languageId,
                persoTagId.AsArray(),
                new[] { creatorId, otherUserId },
                RandomHelper.String());

            var errorMesg = RandomHelper.String();
            var localizer = new TestLocalizer("PersoTagAllowedOnlyOnPrivateCards".PairedWith(errorMesg));
            using var dbContext = new MemCheckDbContext(testDB);
            var exception = await Assert.ThrowsExceptionAsync<PersoTagAllowedOnlyOnPrivateCardsException>(async () => await new UpdateCard(dbContext.AsCallContext(localizer)).RunAsync(request));
            Assert.AreEqual(errorMesg, exception.Message);
        }
        [TestMethod()]
        public async Task AddingPersoTagToPrivateCardMustSucceed()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var creatorId = await UserHelper.CreateInDbAsync(testDB);
            var languageId = await CardLanguagHelper.CreateAsync(testDB);
            var cardId = await CardHelper.CreateIdAsync(testDB, creatorId, language: languageId, userWithViewIds: creatorId.AsArray());

            var persoTagId = await TagHelper.CreateAsync(testDB, name: Tag.Perso);

            var request = new UpdateCard.Request(
                cardId,
                creatorId,
                RandomHelper.String(),
                Array.Empty<Guid>(),
                RandomHelper.String(),
                Array.Empty<Guid>(),
                RandomHelper.String(),
                Array.Empty<Guid>(),
                RandomHelper.String(),
                languageId,
                persoTagId.AsArray(),
                creatorId.AsArray(),
                RandomHelper.String());

            using (var dbContext = new MemCheckDbContext(testDB))
                await new UpdateCard(dbContext.AsCallContext()).RunAsync(request);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var card = dbContext.Cards.Include(card => card.VersionCreator).Single();
                Assert.AreEqual(creatorId, card.VersionCreator.Id);
            }
        }
        [TestMethod()]
        public async Task ChangingAPersoCardToPublicAndAddingPersoTagMustFail()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var creatorId = await UserHelper.CreateInDbAsync(testDB);
            var languageId = await CardLanguagHelper.CreateAsync(testDB);
            var aTagId = await TagHelper.CreateAsync(testDB);
            var cardId = await CardHelper.CreateIdAsync(testDB, creatorId, language: languageId, userWithViewIds: creatorId.AsArray(), tagIds: aTagId.AsArray());

            var persoTagId = await TagHelper.CreateAsync(testDB, name: Tag.Perso);

            var request = new UpdateCard.Request(
                cardId,
                creatorId,
                RandomHelper.String(),
                Array.Empty<Guid>(),
                RandomHelper.String(),
                Array.Empty<Guid>(),
                RandomHelper.String(),
                Array.Empty<Guid>(),
                RandomHelper.String(),
                languageId,
                persoTagId.AsArray(),
                Array.Empty<Guid>(),
                RandomHelper.String());

            var errorMesg = RandomHelper.String();
            var localizer = new TestLocalizer("PersoTagAllowedOnlyOnPrivateCards".PairedWith(errorMesg));
            using var dbContext = new MemCheckDbContext(testDB);
            var exception = await Assert.ThrowsExceptionAsync<PersoTagAllowedOnlyOnPrivateCardsException>(async () => await new UpdateCard(dbContext.AsCallContext(localizer)).RunAsync(request));
            Assert.AreEqual(errorMesg, exception.Message);
        }
        [TestMethod()]
        public async Task ChangingAPersoCardToCardVisibleToOtherAndAddingPersoTagMustFail()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var creatorId = await UserHelper.CreateInDbAsync(testDB);
            var languageId = await CardLanguagHelper.CreateAsync(testDB);
            var aTagId = await TagHelper.CreateAsync(testDB);
            var cardId = await CardHelper.CreateIdAsync(testDB, creatorId, language: languageId, userWithViewIds: creatorId.AsArray(), tagIds: aTagId.AsArray());

            var persoTagId = await TagHelper.CreateAsync(testDB, name: Tag.Perso);
            var otherUserId = await UserHelper.CreateInDbAsync(testDB);

            var request = new UpdateCard.Request(
                cardId,
                creatorId,
                RandomHelper.String(),
                Array.Empty<Guid>(),
                RandomHelper.String(),
                Array.Empty<Guid>(),
                RandomHelper.String(),
                Array.Empty<Guid>(),
                RandomHelper.String(),
                languageId,
                persoTagId.AsArray(),
                new[] { creatorId, otherUserId },
                RandomHelper.String());

            var errorMesg = RandomHelper.String();
            var localizer = new TestLocalizer("PersoTagAllowedOnlyOnPrivateCards".PairedWith(errorMesg));
            using var dbContext = new MemCheckDbContext(testDB);
            var exception = await Assert.ThrowsExceptionAsync<PersoTagAllowedOnlyOnPrivateCardsException>(async () => await new UpdateCard(dbContext.AsCallContext(localizer)).RunAsync(request));
            Assert.AreEqual(errorMesg, exception.Message);
        }
        [TestMethod()]
        public async Task ChangingAPublicCardToPrivateAndAddingPersoTagMustSucceed()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var creatorId = await UserHelper.CreateInDbAsync(testDB);
            var languageId = await CardLanguagHelper.CreateAsync(testDB);
            var cardId = await CardHelper.CreateIdAsync(testDB, creatorId, language: languageId, userWithViewIds: Array.Empty<Guid>());

            var persoTagId = await TagHelper.CreateAsync(testDB, name: Tag.Perso);

            var request = new UpdateCard.Request(
                cardId,
                creatorId,
                RandomHelper.String(),
                Array.Empty<Guid>(),
                RandomHelper.String(),
                Array.Empty<Guid>(),
                RandomHelper.String(),
                Array.Empty<Guid>(),
                RandomHelper.String(),
                languageId,
                persoTagId.AsArray(),
                creatorId.AsArray(),
                RandomHelper.String());

            using (var dbContext = new MemCheckDbContext(testDB))
                await new UpdateCard(dbContext.AsCallContext()).RunAsync(request);

            using (var dbContext = new MemCheckDbContext(testDB))
            {
                var card = dbContext.Cards.Include(card => card.VersionCreator).Single();
                Assert.AreEqual(creatorId, card.VersionCreator.Id);
            }
        }
        [TestMethod()]
        public async Task ChangingAPersoCardWithPersoTagToPublicMustFail()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var creatorId = await UserHelper.CreateInDbAsync(testDB);
            var languageId = await CardLanguagHelper.CreateAsync(testDB);
            var aTagId = await TagHelper.CreateAsync(testDB);
            var persoTagId = await TagHelper.CreateAsync(testDB, name: Tag.Perso);
            var cardId = await CardHelper.CreateIdAsync(testDB, creatorId, language: languageId, userWithViewIds: creatorId.AsArray(), tagIds: new[] { aTagId, persoTagId });

            var request = new UpdateCard.Request(
                cardId,
                creatorId,
                RandomHelper.String(),
                Array.Empty<Guid>(),
                RandomHelper.String(),
                Array.Empty<Guid>(),
                RandomHelper.String(),
                Array.Empty<Guid>(),
                RandomHelper.String(),
                languageId,
                new[] { aTagId, persoTagId },
                Array.Empty<Guid>(),
                RandomHelper.String());

            var errorMesg = RandomHelper.String();
            var localizer = new TestLocalizer("PersoTagAllowedOnlyOnPrivateCards".PairedWith(errorMesg));
            using var dbContext = new MemCheckDbContext(testDB);
            var exception = await Assert.ThrowsExceptionAsync<PersoTagAllowedOnlyOnPrivateCardsException>(async () => await new UpdateCard(dbContext.AsCallContext(localizer)).RunAsync(request));
            Assert.AreEqual(errorMesg, exception.Message);
        }
        [TestMethod()]
        public async Task ChangingAPersoCardWithPersoTagToLimitedVisibilityMustFail()
        {
            var testDB = DbHelper.GetEmptyTestDB();
            var creatorId = await UserHelper.CreateInDbAsync(testDB);
            var languageId = await CardLanguagHelper.CreateAsync(testDB);
            var aTagId = await TagHelper.CreateAsync(testDB);
            var persoTagId = await TagHelper.CreateAsync(testDB, name: Tag.Perso);
            var cardId = await CardHelper.CreateIdAsync(testDB, creatorId, language: languageId, userWithViewIds: creatorId.AsArray(), tagIds: new[] { aTagId, persoTagId });

            var otherUserId = await UserHelper.CreateInDbAsync(testDB);

            var request = new UpdateCard.Request(
                cardId,
                creatorId,
                RandomHelper.String(),
                Array.Empty<Guid>(),
                RandomHelper.String(),
                Array.Empty<Guid>(),
                RandomHelper.String(),
                Array.Empty<Guid>(),
                RandomHelper.String(),
                languageId,
                new[] { aTagId, persoTagId },
                new[] { creatorId, otherUserId },
                RandomHelper.String());

            var errorMesg = RandomHelper.String();
            var localizer = new TestLocalizer("PersoTagAllowedOnlyOnPrivateCards".PairedWith(errorMesg));
            using var dbContext = new MemCheckDbContext(testDB);
            var exception = await Assert.ThrowsExceptionAsync<PersoTagAllowedOnlyOnPrivateCardsException>(async () => await new UpdateCard(dbContext.AsCallContext(localizer)).RunAsync(request));
            Assert.AreEqual(errorMesg, exception.Message);
        }
    }
}
