using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using MemCheck.Application.Searching;
using MemCheck.Application.Users;
using MemCheck.Database;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace MemCheck.AzureFunctions;

// Working on a phone, the adding of hyperlinks is generally made with the mobile version of Wikipedia.
// It makes much more sense to use the desktop version, since the site will automatically switch to the mobile version if suitable.
// Example of mobile link: "https://fr.m.wikipedia.org/wiki/Barbotin", will be replaced with "https://fr.wikipedia.org/wiki/Barbotin"
// Similarly, "https://fr.m.wiktionary.org/wiki/memoire" will be replaced with "https://fr.wiktionary.org/wiki/memoire"

public sealed class MakeWikipediaLinksDesktop : AbstractMemCheckAzureFunction
{
    #region Private methods
    private static string GetMailBody(ImmutableArray<Guid> changedCardGuids, string siteName)
    {
        var writer = new StringBuilder()
            .Append(CultureInfo.InvariantCulture, $"<h1>{changedCardGuids.Length} cards changed for {siteName} links</h1>")
            .Append("<p><ul>");

        foreach (var changedCard in changedCardGuids)
            writer = writer.Append(CultureInfo.InvariantCulture, $"<li>https://www.Mnesios.com/Authoring?CardId={changedCard}</li>");

        writer = writer.Append("</ul></p>");

        return writer.ToString();
    }
    #endregion
    #region Protected override methods
    protected override async Task<string> RunAndCreateReportMailMainPartAsync()
    {
        var callContext = NewCallContext();

        var result = "";
        var replacer = new ReplaceTextInAllVisibleCards(callContext);

        var wikipediaRequest = new ReplaceTextInAllVisibleCards.Request(BotUserId, "https://fr.m.wikipedia.org/wiki/", "https://fr.wikipedia.org/wiki/", "Utilisation de liens Wikipédia par défaut au lieu de la version mobile");
        var wikipediaResult = await replacer.RunAsync(wikipediaRequest);
        result += GetMailBody(wikipediaResult.ChangedCardGuids, "Wikipédia");

        var wiktionaryRequest = new ReplaceTextInAllVisibleCards.Request(BotUserId, "https://fr.m.wiktionary.org/wiki/", "https://fr.wiktionary.org/wiki/", "Utilisation de liens Wiktionnaire par défaut au lieu de la version mobile");
        var wiktionaryResult = await replacer.RunAsync(wiktionaryRequest);
        result += GetMailBody(wiktionaryResult.ChangedCardGuids, "Wiktionnaire");

        return result;
    }
    #endregion
    public MakeWikipediaLinksDesktop(TelemetryConfiguration telemetryConfiguration, MemCheckDbContext memCheckDbContext, MemCheckUserManager userManager, ILogger<SendStatsToAdministrators> logger)
        : base(telemetryConfiguration, memCheckDbContext, userManager, logger)
    {
    }
    [FunctionName(nameof(MakeWikipediaLinksDesktop))]
    public async Task Run([TimerTrigger(Constants.Cron_MakeWikipediaLinksDesktop)] TimerInfo timer, ExecutionContext context)
    {
        await RunAsync(timer, context);
    }
}
