using MemCheck.Application.Helpers;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards;

[TestClass()]
public class PreviousCardVersionCreatorTests
{
    [TestMethod()]
    public async Task TestSameUserCreatesVersion()
    {
        var testDB = DbHelper.GetEmptyTestDB();

        var ownerId = await UserHelper.CreateInDbAsync(testDB);
        var card = await CardHelper.CreateAsync(testDB, ownerId);

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var request = new PreviousCardVersionCreator(dbContext);
            await request.RunAsync(card.Id, ownerId, RandomHelper.String());
            await dbContext.SaveChangesAsync();
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var previousVersion = await dbContext.CardPreviousVersions
                .Include(card => card.VersionCreator)
                .Include(card => card.CardLanguage)
                .Include(card => card.Tags)
                .Include(card => card.UsersWithView)
                .Where(previous => previous.Card == card.Id)
                .SingleAsync();
            Assert.AreNotEqual(card.Id, previousVersion.Id);
            CardComparisonHelper.AssertSameContent(card, previousVersion, true);

        }
    }
    [TestMethod()]
    public async Task TestOtherUserCreatesVersion()
    {
        var testDB = DbHelper.GetEmptyTestDB();

        var ownerId = await UserHelper.CreateInDbAsync(testDB);
        var card = await CardHelper.CreateAsync(testDB, ownerId);
        var newVersionCreatorId = await UserHelper.CreateInDbAsync(testDB, subscribeToCardOnEdit: true);

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var request = new PreviousCardVersionCreator(dbContext);
            await request.RunAsync(card.Id, newVersionCreatorId, RandomHelper.String());
            await dbContext.SaveChangesAsync();
        }

        using (var dbContext = new MemCheckDbContext(testDB))
        {
            var previousVersion = await dbContext.CardPreviousVersions
                .Include(card => card.VersionCreator)
                .Include(card => card.CardLanguage)
                .Include(card => card.Tags)
                .Include(card => card.UsersWithView)
                .Where(previous => previous.Card == card.Id)
                .SingleAsync();
            Assert.AreNotEqual(card.Id, previousVersion.Id);
            CardComparisonHelper.AssertSameContent(card, previousVersion, true);
        }
        Assert.IsTrue(await CardSubscriptionHelper.UserIsSubscribedToCardAsync(testDB, newVersionCreatorId, card.Id));
    }
}
