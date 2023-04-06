using MemCheck.Application.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Database;

// The database has collation SQL_Latin1_General_CP1_CI_AS, ie case insensitive, and the Application code is strictly dependent on that, so let's check

[TestClass()]
public sealed class CollationTests
{
    [TestMethod(), System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Really want to use lower case for test")]
    public async Task TestCardFrontSideIsCaseInsensitive()
    {
        var testDB = DbHelper.GetEmptyTestDB();
        var userId = await UserHelper.CreateInDbAsync(testDB);
        var frontSide = RandomHelper.String(firstCharMustBeLetter: true).ToLowerInvariant();

        var cardWithExactFrontSideId = await CardHelper.CreateIdAsync(testDB, userId, frontSide: frontSide);
        var cardWithPartialFrontSideId = await CardHelper.CreateIdAsync(testDB, userId, frontSide: RandomHelper.String() + frontSide + RandomHelper.String());
        await CardHelper.CreateIdAsync(testDB, userId); // Must not be found

        using var dbContext = new MemCheckDbContext(testDB);
        Assert.AreEqual(cardWithExactFrontSideId, dbContext.Cards.Where(c => c.FrontSide == frontSide).Single().Id);
        Assert.AreEqual(cardWithExactFrontSideId, dbContext.Cards.Where(c => c.FrontSide == frontSide.ToUpperInvariant()).Single().Id);
        Assert.AreEqual(cardWithExactFrontSideId, dbContext.Cards.Where(c => EF.Functions.Like(c.FrontSide, frontSide)).Single().Id);
        Assert.AreEqual(2, dbContext.Cards.Where(c => EF.Functions.Like(c.FrontSide, $"%{frontSide}%")).Count());
    }
}
