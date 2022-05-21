using MemCheck.Application.Helpers;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Languages;

[TestClass()]
public class GetAllLanguagesTests
{
    [TestMethod()]
    public async Task None()
    {
        using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
        var result = await new GetAllLanguages(dbContext.AsCallContext()).RunAsync(new GetAllLanguages.Request());
        Assert.IsFalse(result.Any());
    }
    [TestMethod()]
    public async Task OneLanguageInDb()
    {
        var db = DbHelper.GetEmptyTestDB();
        var name = RandomHelper.String();
        var id = await CardLanguageHelper.CreateAsync(db, name);

        using var dbContext = new MemCheckDbContext(db);
        var result = await new GetAllLanguages(dbContext.AsCallContext()).RunAsync(new GetAllLanguages.Request());
        var resultLang = result.Single();
        Assert.AreEqual(id, resultLang.Id);
        Assert.AreEqual(name, resultLang.Name);
        Assert.AreEqual(0, resultLang.CardCount);
    }
    [TestMethod()]
    public async Task Complex()
    {
        var db = DbHelper.GetEmptyTestDB();
        var language1Name = RandomHelper.String();
        var language1 = await CardLanguageHelper.CreateAsync(db, language1Name);
        var language2Name = RandomHelper.String();
        var language2 = await CardLanguageHelper.CreateAsync(db, language2Name);
        var user = await UserHelper.CreateInDbAsync(db);
        await CardHelper.CreateAsync(db, versionCreatorId: user, language: language1);
        await CardHelper.CreateAsync(db, versionCreatorId: user, language: language1);
        await CardHelper.CreateAsync(db, versionCreatorId: user, language: language1);
        await CardHelper.CreateAsync(db, versionCreatorId: user, language: language2);

        using var dbContext = new MemCheckDbContext(db);
        var result = await new GetAllLanguages(dbContext.AsCallContext()).RunAsync(new GetAllLanguages.Request());

        var resultLang1 = result.Single(l => l.Id == language1);
        Assert.AreEqual(language1Name, resultLang1.Name);
        Assert.AreEqual(3, resultLang1.CardCount);

        var resultLang2 = result.Single(l => l.Id == language2);
        Assert.AreEqual(language2Name, resultLang2.Name);
        Assert.AreEqual(1, resultLang2.CardCount);
    }
}
