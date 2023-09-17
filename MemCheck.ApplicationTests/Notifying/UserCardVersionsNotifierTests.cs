using MemCheck.Application.Cards;
using MemCheck.Application.Helpers;
using MemCheck.Application.Notifiying;
using MemCheck.Basics;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Notifying;

[TestClass()]
public class UserCardVersionsNotifierTests
{
    #region Private methods
    private static async Task<CardPreviousVersion> CreateCardPreviousVersionAsync(DbContextOptions<MemCheckDbContext> testDB, Guid versionCreatorId, Guid cardId, DateTime versionDate)
    {
        var cardLanguageId = await CardLanguageHelper.CreateAsync(testDB);

        using var dbContext = new MemCheckDbContext(testDB);
        var creator = await dbContext.Users.Where(u => u.Id == versionCreatorId).SingleAsync();
        var cardLanguage = await dbContext.CardLanguages.SingleAsync(cardLanguage => cardLanguage.Id == cardLanguageId);

        var result = new CardPreviousVersion
        {
            Card = cardId,
            CardLanguage = cardLanguage,
            VersionCreator = creator,
            VersionUtcDate = versionDate,
            VersionType = CardPreviousVersionType.Creation,
            FrontSide = RandomHelper.String(),
            BackSide = RandomHelper.String(),
            AdditionalInfo = RandomHelper.String(),
            References = RandomHelper.String(),
            VersionDescription = RandomHelper.String()
        };
        dbContext.CardPreviousVersions.Add(result);

        var card = await dbContext.Cards.Where(c => c.Id == cardId).SingleAsync();
        card.PreviousVersion = result;
        card.VersionType = CardVersionType.Changes;

        await dbContext.SaveChangesAsync();
        return result;
    }
    private static async Task<CardPreviousVersion> CreatePreviousVersionPreviousVersionAsync(DbContextOptions<MemCheckDbContext> testDB, Guid versionCreatorId, CardPreviousVersion previousVersion, DateTime versionDate)
    {
        using var dbContext = new MemCheckDbContext(testDB);
        var creator = await dbContext.Users.Where(u => u.Id == versionCreatorId).SingleAsync();
        var cardLanguage = await dbContext.CardLanguages.SingleAsync(cardLanguage => cardLanguage.Id == previousVersion.CardLanguage.Id);

        var result = new CardPreviousVersion
        {
            Card = previousVersion.Card,
            CardLanguage = cardLanguage,
            VersionCreator = creator,
            VersionUtcDate = versionDate,
            VersionType = CardPreviousVersionType.Creation,
            FrontSide = RandomHelper.String(),
            BackSide = RandomHelper.String(),
            AdditionalInfo = RandomHelper.String(),
            References = RandomHelper.String(),
            VersionDescription = RandomHelper.String()
        };
        dbContext.CardPreviousVersions.Add(result);

        previousVersion.PreviousVersion = result;
        previousVersion.VersionType = CardPreviousVersionType.Changes;

        await dbContext.SaveChangesAsync();
        return result;
    }
    #endregion
    [TestMethod()]
    public async Task EmptyDB()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var user1 = await UserHelper.CreateInDbAsync(testDB);

        using var dbContext = new MemCheckDbContext(testDB);
        var versions = await new UserCardVersionsNotifier(dbContext.AsCallContext(), DateTime.UtcNow).RunAsync(user1);
        Assert.AreEqual(0, versions.Cards.Length);
    }
    [TestMethod()]
    public async Task CardWithoutPreviousVersion_CardExistedOnLastNotif_NoDiscussionEntry()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(testDB);
        var card = await CardHelper.CreateAsync(testDB, user, new DateTime(2020, 11, 2));
        var lastNotificationDate = new DateTime(2020, 11, 3);
        await CardSubscriptionHelper.CreateAsync(testDB, user, card.Id, lastNotificationDate);

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var versions = await new UserCardVersionsNotifier(dbContext.AsCallContext(), new DateTime(2020, 11, 5)).RunAsync(user);
            Assert.AreEqual(0, versions.Cards.Length);
        }

        using (var dbContext = new MemCheckDbContext(testDB))
            Assert.AreEqual(lastNotificationDate, dbContext.CardNotifications.Single().LastNotificationUtcDate);
    }
    [TestMethod()]
    public async Task CardWithoutPreviousVersion_CardExistedOnLastNotif_DiscussionEntryExistedOnLastNotif()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(testDB);
        var card = await CardHelper.CreateAsync(testDB, userId, new DateTime(2020, 11, 2));

        using (var dbContext = new MemCheckDbContext(testDB))
            await new AddEntryToCardDiscussion(dbContext.AsCallContext(), new DateTime(2020, 11, 3)).RunAsync(new AddEntryToCardDiscussion.Request(userId, card.Id, RandomHelper.String()));

        var lastNotificationDate = new DateTime(2020, 11, 4);
        await CardSubscriptionHelper.CreateAsync(testDB, userId, card.Id, lastNotificationDate);

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var versions = await new UserCardVersionsNotifier(dbContext.AsCallContext(), new DateTime(2020, 11, 5)).RunAsync(userId);
            Assert.AreEqual(0, versions.Cards.Length);
        }

        using (var dbContext = new MemCheckDbContext(testDB))
            Assert.AreEqual(lastNotificationDate, dbContext.CardNotifications.Single().LastNotificationUtcDate);
    }
    [TestMethod()]
    public async Task CardWithoutPreviousVersion_CardExistedOnLastNotif_FirstDiscussionEntryAppearedSinceLastNotif()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(testDB);
        var card = await CardHelper.CreateAsync(testDB, user.Id, new DateTime(2020, 11, 2));
        await CardSubscriptionHelper.CreateAsync(testDB, user.Id, card.Id, new DateTime(2020, 11, 3));

        var entryText = RandomHelper.String();
        var entryDate = new DateTime(2020, 11, 4);

        using (var dbContext = new MemCheckDbContext(testDB))
            await new AddEntryToCardDiscussion(dbContext.AsCallContext(), entryDate).RunAsync(new AddEntryToCardDiscussion.Request(user.Id, card.Id, entryText));

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var runDate = new DateTime(2020, 11, 5);
            var versions = await new UserCardVersionsNotifier(dbContext.AsCallContext(), runDate).RunAsync(user.Id);
            Assert.AreEqual(1, versions.Cards.Length);
            Assert.AreEqual(0, versions.Cards.Single().CardVersions.Length);
            Assert.AreEqual(1, versions.Cards.Single().DiscussionEntries.Length);

            var entry = versions.Cards.Single().DiscussionEntries.Single();
            Assert.AreEqual(entryText, entry.Text);
            Assert.AreEqual(entryDate, entry.CreationUtcDate);
            Assert.AreEqual(user.UserName, entry.VersionCreator);

            Assert.AreEqual(runDate, dbContext.CardNotifications.Single().LastNotificationUtcDate);
        }
    }
    [TestMethod()]
    public async Task CardWithoutPreviousVersion_CardExistedOnLastNotif_SecondDiscussionEntryAppearedSinceLastNotif()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(testDB);
        var card = await CardHelper.CreateAsync(testDB, user.Id, new DateTime(2020, 11, 1));
        using (var dbContext = new MemCheckDbContext(testDB))
            await new AddEntryToCardDiscussion(dbContext.AsCallContext(), new DateTime(2020, 11, 2)).RunAsync(new AddEntryToCardDiscussion.Request(user.Id, card.Id, RandomHelper.String()));
        var lastNotificationDate = new DateTime(2020, 11, 3);
        await CardSubscriptionHelper.CreateAsync(testDB, user.Id, card.Id, lastNotificationDate);

        var entryText = RandomHelper.String();
        var entryDate = new DateTime(2020, 11, 4);

        using (var dbContext = new MemCheckDbContext(testDB))
            await new AddEntryToCardDiscussion(dbContext.AsCallContext(), entryDate).RunAsync(new AddEntryToCardDiscussion.Request(user.Id, card.Id, entryText));

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var runDate = new DateTime(2020, 11, 5);
            var versions = await new UserCardVersionsNotifier(dbContext.AsCallContext(), runDate).RunAsync(user.Id);
            Assert.AreEqual(1, versions.Cards.Length);
            Assert.AreEqual(0, versions.Cards.Single().CardVersions.Length);
            Assert.AreEqual(1, versions.Cards.Single().DiscussionEntries.Length);

            var entry = versions.Cards.Single().DiscussionEntries.Single();
            Assert.AreEqual(entryText, entry.Text);
            Assert.AreEqual(entryDate, entry.CreationUtcDate);
            Assert.AreEqual(user.UserName, entry.VersionCreator);

            Assert.AreEqual(runDate, dbContext.CardNotifications.Single().LastNotificationUtcDate);
        }
    }
    [TestMethod()]
    public async Task CardWithoutPreviousVersion_CardCreatedSinceLastNotif_NoDiscussionEntry()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(testDB);
        var card = await CardHelper.CreateAsync(testDB, user, new DateTime(2020, 11, 2));
        await CardSubscriptionHelper.CreateAsync(testDB, user, card.Id, new DateTime(2020, 11, 1));

        var runDate = new DateTime(2020, 11, 3);

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var cardVersionsNotifierResult = await new UserCardVersionsNotifier(dbContext.AsCallContext(), runDate).RunAsync(user);
            Assert.AreEqual(1, cardVersionsNotifierResult.Cards.Length);
            var versions = cardVersionsNotifierResult.Cards.Single().CardVersions;
            Assert.AreEqual(1, versions.Length);
            Assert.AreEqual(card.Id, versions[0].CardId);
            Assert.IsTrue(versions[0].CardIsViewable);
            Assert.AreEqual(card.FrontSide, versions[0].FrontSide);
            Assert.AreEqual(card.VersionDescription, versions[0].VersionDescription);
            Assert.IsNull(versions[0].VersionIdOnLastNotification);
            Assert.AreEqual(0, cardVersionsNotifierResult.Cards.Single().DiscussionEntries.Length);
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var notif = dbContext.CardNotifications.Single(cn => cn.CardId == card.Id && cn.UserId == user);
            Assert.AreEqual(runDate, notif.LastNotificationUtcDate);
        }
    }
    [TestMethod()]
    public async Task CardWithoutPreviousVersion_CardCreatedSinceLastNotif_OneDiscussionEntry()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var userName = RandomHelper.UserName();
        var user = await UserHelper.CreateInDbAsync(testDB, userName: userName);
        var card = await CardHelper.CreateAsync(testDB, user, new DateTime(2020, 11, 2));
        var entryText = RandomHelper.String();
        await CardSubscriptionHelper.CreateAsync(testDB, user, card.Id, new DateTime(2020, 11, 1));

        var entryDate = new DateTime(2020, 11, 4);
        using (var dbContext = new MemCheckDbContext(testDB))
            await new AddEntryToCardDiscussion(dbContext.AsCallContext(), entryDate).RunAsync(new AddEntryToCardDiscussion.Request(user, card.Id, entryText));

        var runDate = new DateTime(2020, 11, 10);

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var cardVersionsNotifierResult = await new UserCardVersionsNotifier(dbContext.AsCallContext(), runDate).RunAsync(user);
            { // Check the card version
                var versions = cardVersionsNotifierResult.Cards.Single().CardVersions;
                Assert.AreEqual(1, versions.Length);
                Assert.AreEqual(card.Id, versions[0].CardId);
                Assert.IsTrue(versions[0].CardIsViewable);
                Assert.AreEqual(card.FrontSide, versions[0].FrontSide);
                Assert.AreEqual(card.VersionDescription, versions[0].VersionDescription);
                Assert.IsNull(versions[0].VersionIdOnLastNotification);
            }
            { // Check the card discussion
                Assert.AreEqual(1, cardVersionsNotifierResult.Cards.Single().DiscussionEntries.Length);
                var entry = cardVersionsNotifierResult.Cards.Single().DiscussionEntries.Single();
                Assert.AreEqual(entryText, entry.Text);
                Assert.AreEqual(entryDate, entry.CreationUtcDate);
                Assert.AreEqual(userName, entry.VersionCreator);
            }
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var notif = dbContext.CardNotifications.Single(cn => cn.CardId == card.Id && cn.UserId == user);
            Assert.AreEqual(runDate, notif.LastNotificationUtcDate);
        }
    }
    [TestMethod()]
    public async Task CardWithoutPreviousVersion_CardCreatedSinceLastNotif_TwoDiscussionEntries()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var userName = RandomHelper.UserName();
        var user = await UserHelper.CreateInDbAsync(testDB, userName: userName);
        var card = await CardHelper.CreateAsync(testDB, user, new DateTime(2020, 11, 2));
        using (var dbContext = new MemCheckDbContext(testDB))
            await new AddEntryToCardDiscussion(dbContext.AsCallContext(), new DateTime(2020, 11, 2)).RunAsync(new AddEntryToCardDiscussion.Request(user, card.Id, RandomHelper.String()));
        var entryText = RandomHelper.String();
        var entryDate = new DateTime(2020, 11, 4);
        using (var dbContext = new MemCheckDbContext(testDB))
            await new AddEntryToCardDiscussion(dbContext.AsCallContext(), entryDate).RunAsync(new AddEntryToCardDiscussion.Request(user, card.Id, entryText));

        await CardSubscriptionHelper.CreateAsync(testDB, user, card.Id, new DateTime(2020, 11, 1));

        var runDate = new DateTime(2020, 11, 10);

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var cardVersionsNotifierResult = await new UserCardVersionsNotifier(dbContext.AsCallContext(), runDate).RunAsync(user);
            { // Check the card version
                var versions = cardVersionsNotifierResult.Cards.Single().CardVersions;
                Assert.AreEqual(1, versions.Length);
                Assert.AreEqual(card.Id, versions[0].CardId);
                Assert.IsTrue(versions[0].CardIsViewable);
                Assert.AreEqual(card.FrontSide, versions[0].FrontSide);
                Assert.AreEqual(card.VersionDescription, versions[0].VersionDescription);
                Assert.IsNull(versions[0].VersionIdOnLastNotification);
            }
            { // Check the card discussion
                Assert.AreEqual(1, cardVersionsNotifierResult.Cards.Single().DiscussionEntries.Length);
                var entry = cardVersionsNotifierResult.Cards.Single().DiscussionEntries.Single();
                Assert.AreEqual(entryText, entry.Text);
                Assert.AreEqual(entryDate, entry.CreationUtcDate);
                Assert.AreEqual(userName, entry.VersionCreator);
            }
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var notif = dbContext.CardNotifications.Single(cn => cn.CardId == card.Id && cn.UserId == user);
            Assert.AreEqual(runDate, notif.LastNotificationUtcDate);
        }
    }
    [TestMethod()]
    public async Task CardWithOnePreviousVersion_NotToBeNotifiedBecauseOfDate()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var user1 = await UserHelper.CreateInDbAsync(testDB);
        var card = await CardHelper.CreateAsync(testDB, user1, new DateTime(2020, 11, 2));
        var user2 = await UserHelper.CreateInDbAsync(testDB);
        await CreateCardPreviousVersionAsync(testDB, user2, card.Id, new DateTime(2020, 11, 1));
        var lastNotificationDate = new DateTime(2020, 11, 3);
        await CardSubscriptionHelper.CreateAsync(testDB, user1, card.Id, lastNotificationDate);

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var versions = await new UserCardVersionsNotifier(dbContext.AsCallContext(), new DateTime(2020, 11, 5)).RunAsync(user1);
            Assert.AreEqual(0, versions.Cards.Length);
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var notif = dbContext.CardNotifications.Single(cn => cn.CardId == card.Id && cn.UserId == user1);
            Assert.IsTrue(notif.LastNotificationUtcDate == lastNotificationDate);
        }
    }
    [TestMethod()]
    public async Task CardWithOnePreviousVersionAndDiscussionEntry_NotToBeNotifiedBecauseOfDate()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var user1 = await UserHelper.CreateInDbAsync(testDB);
        var card = await CardHelper.CreateAsync(testDB, user1, new DateTime(2020, 11, 2));
        var user2 = await UserHelper.CreateInDbAsync(testDB);
        await CreateCardPreviousVersionAsync(testDB, user2, card.Id, new DateTime(2020, 11, 1));

        using (var dbContext = new MemCheckDbContext(testDB))
            await new AddEntryToCardDiscussion(dbContext.AsCallContext(), new DateTime(2020, 11, 3)).RunAsync(new AddEntryToCardDiscussion.Request(user2, card.Id, RandomHelper.String()));

        var lastNotificationDate = new DateTime(2020, 11, 4);
        await CardSubscriptionHelper.CreateAsync(testDB, user1, card.Id, lastNotificationDate);

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var versions = await new UserCardVersionsNotifier(dbContext.AsCallContext(), new DateTime(2020, 11, 5)).RunAsync(user1);
            Assert.AreEqual(0, versions.Cards.Length);
        }

        using (var dbContext = new MemCheckDbContext(testDB))
            Assert.IsTrue(dbContext.CardNotifications.Single().LastNotificationUtcDate == lastNotificationDate);
    }
    [TestMethod()]
    public async Task CardWithOnePreviousVersion_ToBeNotifiedWithoutAccessibility_NoDiscussion()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var user1 = await UserHelper.CreateUserInDbAsync(testDB);
        var user2 = await UserHelper.CreateInDbAsync(testDB);

        // user1 creates card, public
        var card = await CardHelper.CreateAsync(testDB, user1, new DateTime(2020, 11, 2));

        // user1 subscribes to card
        await CardSubscriptionHelper.CreateAsync(testDB, user1.Id, card.Id, new DateTime(2020, 11, 3));

        // user2 subscribes to card
        await CardSubscriptionHelper.CreateAsync(testDB, user2, card.Id, new DateTime(2020, 11, 3));

        // user1 makes card private
        var updateDate = new DateTime(2020, 11, 4);
        var newVersionDescription = RandomHelper.String();
        await UpdateCardHelper.RunAsync(testDB, UpdateCardHelper.RequestForVisibilityChange(card, user1.Id.AsArray(), versionDescription: newVersionDescription), updateDate);

        // The public version of the card is now a previous version
        var publicVersion = await CardPreviousVersionHelper.GetPreviousVersionAsync(testDB, card.Id);
        Assert.IsFalse(publicVersion!.UsersWithView.Any());

        using var dbContext = new MemCheckDbContext(testDB);
        var runDate = new DateTime(2020, 11, 5);
        var notifier = new UserCardVersionsNotifier(dbContext.AsCallContext(), runDate);

        // user1 receives notification
        var user1Notifications = await notifier.RunAsync(user1.Id);
        Assert.AreEqual(1, user1Notifications.Cards.Length);
        Assert.AreEqual(1, user1Notifications.Cards.Single().CardVersions.Length);
        Assert.AreEqual(0, user1Notifications.Cards.Single().DiscussionEntries.Length);
        Assert.AreEqual(card.Id, user1Notifications.Cards.Single().CardVersions[0].CardId);
        Assert.AreEqual(card.FrontSide, user1Notifications.Cards.Single().CardVersions[0].FrontSide);
        Assert.AreEqual(user1.UserName, user1Notifications.Cards.Single().CardVersions[0].VersionCreator);
        Assert.AreEqual(updateDate, user1Notifications.Cards.Single().CardVersions[0].VersionUtcDate);
        Assert.AreEqual(newVersionDescription, user1Notifications.Cards.Single().CardVersions[0].VersionDescription);
        Assert.IsTrue(user1Notifications.Cards.Single().CardVersions[0].CardIsViewable);
        Assert.AreEqual(publicVersion.Id, user1Notifications.Cards.Single().CardVersions[0].VersionIdOnLastNotification);

        var user2Notifications = await notifier.RunAsync(user2);
        var user2versions = user2Notifications.Cards.Single().CardVersions;
        Assert.AreEqual(1, user2versions.Length);
        Assert.AreEqual(card.Id, user2versions[0].CardId);
        Assert.IsFalse(user2versions[0].CardIsViewable);
        Assert.IsNull(user2versions[0].VersionDescription);
        Assert.IsNull(user2versions[0].FrontSide);
        Assert.AreEqual(publicVersion.Id, user2versions[0].VersionIdOnLastNotification);

        var notif = dbContext.CardNotifications.Single(cn => cn.CardId == card.Id && cn.UserId == user2);
        Assert.AreEqual(runDate, notif.LastNotificationUtcDate);
    }
    [TestMethod()]
    public async Task CardWithOnePreviousVersionButNoNewDiscussionEntry_ToBeNotifiedWithAccessibility()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var user1 = await UserHelper.CreateInDbAsync(testDB);
        var user2 = await UserHelper.CreateInDbAsync(testDB);

        var card = await CardHelper.CreateAsync(testDB, user1, new DateTime(2020, 11, 2), new Guid[] { user1, user2 });
        var previousVersion = await CreateCardPreviousVersionAsync(testDB, user1, card.Id, new DateTime(2020, 11, 1));

        await CardSubscriptionHelper.CreateAsync(testDB, user2, card.Id, new DateTime(2020, 11, 1));
        var now = new DateTime(2020, 11, 3);

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var versions = await new UserCardVersionsNotifier(dbContext.AsCallContext(), now).RunAsync(user2);
            Assert.AreEqual(1, versions.Cards.Single().CardVersions.Length);
            Assert.AreEqual(card.Id, versions.Cards.Single().CardVersions[0].CardId);
            Assert.IsTrue(versions.Cards.Single().CardVersions[0].CardIsViewable);
            Assert.AreEqual(previousVersion.Id, versions.Cards.Single().CardVersions[0].VersionIdOnLastNotification);
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var notif = dbContext.CardNotifications.Single(cn => cn.CardId == card.Id && cn.UserId == user2);
            Assert.AreEqual(now, notif.LastNotificationUtcDate);
        }
    }
    [TestMethod()]
    public async Task CardWithOnePreviousVersion_ToBeNotified_LastNotifAfterInitialCreation()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var user1 = await UserHelper.CreateInDbAsync(testDB);
        var card = await CardHelper.CreateAsync(testDB, user1, new DateTime(2020, 11, 3));
        var user2 = await UserHelper.CreateInDbAsync(testDB);
        var previousVersion = await CreateCardPreviousVersionAsync(testDB, user2, card.Id, new DateTime(2020, 11, 1));
        await CardSubscriptionHelper.CreateAsync(testDB, user1, card.Id, new DateTime(2020, 11, 2));
        var now = new DateTime(2020, 11, 4);

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var notifier = new UserCardVersionsNotifier(dbContext.AsCallContext(), now);
            var versions = await notifier.RunAsync(user1);
            Assert.AreEqual(1, versions.Cards.Single().CardVersions.Length);
            Assert.AreEqual(card.Id, versions.Cards.Single().CardVersions[0].CardId);
            Assert.IsTrue(versions.Cards.Single().CardVersions[0].CardIsViewable);
            Assert.AreEqual(previousVersion.Id, versions.Cards.Single().CardVersions[0].VersionIdOnLastNotification);
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var notif = dbContext.CardNotifications.Single(cn => cn.CardId == card.Id && cn.UserId == user1);
            Assert.AreEqual(now, notif.LastNotificationUtcDate);
        }
    }
    [TestMethod()]
    public async Task CardWithOnePreviousVersion_ToBeNotified_LastNotifBeforeInitialCreation()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var user1 = await UserHelper.CreateInDbAsync(testDB);
        var card = await CardHelper.CreateAsync(testDB, user1, new DateTime(2020, 11, 3));
        var user2 = await UserHelper.CreateInDbAsync(testDB);
        await CreateCardPreviousVersionAsync(testDB, user2, card.Id, new DateTime(2020, 11, 2));
        await CardSubscriptionHelper.CreateAsync(testDB, user1, card.Id, new DateTime(2020, 11, 1));
        var now = new DateTime(2020, 11, 4);

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var notifier = new UserCardVersionsNotifier(dbContext.AsCallContext(), now);
            var versions = await notifier.RunAsync(user1);
            Assert.AreEqual(1, versions.Cards.Single().CardVersions.Length);
            Assert.AreEqual(card.Id, versions.Cards.Single().CardVersions[0].CardId);
            Assert.IsTrue(versions.Cards.Single().CardVersions[0].CardIsViewable);
            Assert.IsNull(versions.Cards.Single().CardVersions[0].VersionIdOnLastNotification);
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var notif = dbContext.CardNotifications.Single(cn => cn.CardId == card.Id && cn.UserId == user1);
            Assert.AreEqual(now, notif.LastNotificationUtcDate);
        }
    }
    [TestMethod()]
    public async Task CardWithPreviousVersions_NotToBeNotified_BecauseLastNotifAfterVersion()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var user1 = await UserHelper.CreateInDbAsync(testDB);
        var card = await CardHelper.CreateAsync(testDB, user1, new DateTime(2020, 11, 3));
        var user2 = await UserHelper.CreateInDbAsync(testDB);
        var previousVersion1 = await CreateCardPreviousVersionAsync(testDB, user2, card.Id, new DateTime(2020, 11, 2));
        await CreatePreviousVersionPreviousVersionAsync(testDB, user2, previousVersion1, new DateTime(2020, 11, 1));
        var lastNotificationDate = new DateTime(2020, 11, 3);
        await CardSubscriptionHelper.CreateAsync(testDB, user1, card.Id, lastNotificationDate);

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var versions = await new UserCardVersionsNotifier(dbContext.AsCallContext(), new DateTime(2020, 11, 5)).RunAsync(user1);
            Assert.AreEqual(0, versions.Cards.Length);
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var notif = dbContext.CardNotifications.Single(cn => cn.CardId == card.Id && cn.UserId == user1);
            Assert.AreEqual(lastNotificationDate, notif.LastNotificationUtcDate);
        }
    }
    [TestMethod()]
    public async Task CardWithPreviousVersions_ToBeNotified_LastNotifAfterPreviousVersionCreation()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var user1 = await UserHelper.CreateInDbAsync(testDB);
        var card = await CardHelper.CreateAsync(testDB, user1, new DateTime(2020, 11, 5));
        var user2 = await UserHelper.CreateInDbAsync(testDB);
        var previousVersion1 = await CreateCardPreviousVersionAsync(testDB, user2, card.Id, new DateTime(2020, 11, 3));
        await CreatePreviousVersionPreviousVersionAsync(testDB, user2, previousVersion1, new DateTime(2020, 11, 1));
        await CardSubscriptionHelper.CreateAsync(testDB, user1, card.Id, new DateTime(2020, 11, 4));
        var now = new DateTime(2020, 11, 5);

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var versions = await new UserCardVersionsNotifier(dbContext.AsCallContext(), now).RunAsync(user1);
            Assert.AreEqual(1, versions.Cards.Single().CardVersions.Length);
            Assert.AreEqual(card.Id, versions.Cards.Single().CardVersions[0].CardId);
            Assert.IsTrue(versions.Cards.Single().CardVersions[0].CardIsViewable);
            Assert.AreEqual(previousVersion1.Id, versions.Cards.Single().CardVersions[0].VersionIdOnLastNotification);
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var notif = dbContext.CardNotifications.Single(cn => cn.CardId == card.Id && cn.UserId == user1);
            Assert.AreEqual(now, notif.LastNotificationUtcDate);
        }
    }
    [TestMethod()]
    public async Task CardWithPreviousVersions_ToBeNotified_LastNotifBeforePreviousVersionCreation()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var user1 = await UserHelper.CreateInDbAsync(testDB);
        var card = await CardHelper.CreateAsync(testDB, user1, new DateTime(2020, 11, 5));
        var user2 = await UserHelper.CreateInDbAsync(testDB);
        var previousVersion1 = await CreateCardPreviousVersionAsync(testDB, user2, card.Id, new DateTime(2020, 11, 3));
        var oldestVersion = await CreatePreviousVersionPreviousVersionAsync(testDB, user2, previousVersion1, new DateTime(2020, 11, 1));
        await CardSubscriptionHelper.CreateAsync(testDB, user1, card.Id, new DateTime(2020, 11, 2));
        var now = new DateTime(2020, 11, 5);

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var cardVersionsNotifierResult = await new UserCardVersionsNotifier(dbContext.AsCallContext(), now).RunAsync(user1);
            var versions = cardVersionsNotifierResult.Cards.Single().CardVersions;
            Assert.AreEqual(1, versions.Length);
            Assert.AreEqual(card.Id, versions[0].CardId);
            Assert.IsTrue(versions[0].CardIsViewable);
            Assert.AreEqual(oldestVersion.Id, versions[0].VersionIdOnLastNotification);
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var notif = dbContext.CardNotifications.Single(cn => cn.CardId == card.Id && cn.UserId == user1);
            Assert.AreEqual(now, notif.LastNotificationUtcDate);
        }
    }
    [TestMethod()]
    public async Task MultipleVersions()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var user1 = await UserHelper.CreateInDbAsync(testDB);
        var user2 = await UserHelper.CreateInDbAsync(testDB);

        var card1 = await CardHelper.CreateAsync(testDB, user1, new DateTime(2020, 11, 10));
        var card1PV1 = await CreateCardPreviousVersionAsync(testDB, user2, card1.Id, new DateTime(2020, 11, 5));
        var card1PV2 = await CreatePreviousVersionPreviousVersionAsync(testDB, user2, card1PV1, new DateTime(2020, 11, 1));
        var card1Oldest = await CreatePreviousVersionPreviousVersionAsync(testDB, user2, card1PV2, new DateTime(2020, 10, 15));
        await CardSubscriptionHelper.CreateAsync(testDB, user1, card1.Id, new DateTime(2020, 11, 2));

        var card2 = await CardHelper.CreateAsync(testDB, user1, new DateTime(2020, 11, 10));
        var card2PV1 = await CreateCardPreviousVersionAsync(testDB, user2, card2.Id, new DateTime(2020, 11, 5));
        var card2Oldest = await CreatePreviousVersionPreviousVersionAsync(testDB, user2, card2PV1, new DateTime(2020, 11, 2));
        await CardSubscriptionHelper.CreateAsync(testDB, user1, card2.Id, new DateTime(2020, 11, 1));

        var card3 = await CardHelper.CreateAsync(testDB, user1, new DateTime(2020, 11, 10));
        var card3PV1 = await CreateCardPreviousVersionAsync(testDB, user2, card3.Id, new DateTime(2020, 11, 5));
        await CreatePreviousVersionPreviousVersionAsync(testDB, user2, card3PV1, new DateTime(2020, 11, 2));
        var card3LastNotifDate = new DateTime(2020, 11, 11);
        await CardSubscriptionHelper.CreateAsync(testDB, user1, card3.Id, card3LastNotifDate);

        var card4 = await CardHelper.CreateAsync(testDB, user2, new DateTime(2020, 11, 10), new Guid[] { user2 });  //Not to be notified because no access for user1
        await CreateCardPreviousVersionAsync(testDB, user2, card4.Id, new DateTime(2020, 11, 5));
        await CardSubscriptionHelper.CreateAsync(testDB, user1, card4.Id, new DateTime(2020, 11, 2));

        var card5 = await CardHelper.CreateAsync(testDB, user2, new DateTime(2020, 11, 10), new Guid[] { user1, user2 });
        await CreateCardPreviousVersionAsync(testDB, user2, card5.Id, new DateTime(2020, 11, 5));
        await CardSubscriptionHelper.CreateAsync(testDB, user1, card5.Id, new DateTime(2020, 11, 2));

        var now = new DateTime(2020, 11, 5);

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var user1CardVersionsNotifierResult = await new UserCardVersionsNotifier(dbContext.AsCallContext(), now).RunAsync(user1);
            Assert.AreEqual(4, user1CardVersionsNotifierResult.Cards.Length);
            //var user1CardVersions = user1CardVersionsNotifierResult.Cards.Single().CardVersions;

            //Assert.AreEqual(4, user1CardVersions.Length);

            var notifForCard1 = user1CardVersionsNotifierResult.Cards.Where(v => v.CardId == card1.Id).Single();
            Assert.IsTrue(notifForCard1.CardVersions.Single().CardIsViewable);
            Assert.AreEqual(card1PV2.Id, notifForCard1.CardVersions.Single().VersionIdOnLastNotification);

            var notifForCard2 = user1CardVersionsNotifierResult.Cards.Where(v => v.CardId == card2.Id).Single();
            Assert.IsTrue(notifForCard2.CardVersions.Single().CardIsViewable);
            Assert.IsNull(notifForCard2.CardVersions.Single().VersionIdOnLastNotification);

            Assert.IsFalse(user1CardVersionsNotifierResult.Cards.Any(v => v.CardId == card3.Id));

            var notifForCard4 = user1CardVersionsNotifierResult.Cards.Where(v => v.CardId == card4.Id).Single();
            Assert.IsFalse(notifForCard4.CardVersions.Single().CardIsViewable);
            Assert.IsNull(notifForCard4.CardVersions.Single().VersionIdOnLastNotification);

            var notifForCard5 = user1CardVersionsNotifierResult.Cards.Where(v => v.CardId == card5.Id).Single();
            Assert.IsTrue(notifForCard5.CardVersions.Single().CardIsViewable);
            Assert.IsNull(notifForCard5.CardVersions.Single().VersionIdOnLastNotification);
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            Assert.AreEqual(now, dbContext.CardNotifications.Single(cn => cn.CardId == card1.Id && cn.UserId == user1).LastNotificationUtcDate);
            Assert.AreEqual(now, dbContext.CardNotifications.Single(cn => cn.CardId == card2.Id && cn.UserId == user1).LastNotificationUtcDate);
            Assert.AreEqual(card3LastNotifDate, dbContext.CardNotifications.Single(cn => cn.CardId == card3.Id && cn.UserId == user1).LastNotificationUtcDate);
            Assert.AreEqual(now, dbContext.CardNotifications.Single(cn => cn.CardId == card4.Id && cn.UserId == user1).LastNotificationUtcDate);
            Assert.AreEqual(now, dbContext.CardNotifications.Single(cn => cn.CardId == card5.Id && cn.UserId == user1).LastNotificationUtcDate);
        }
    }
}
