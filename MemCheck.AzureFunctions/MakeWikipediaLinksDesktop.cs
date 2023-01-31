using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using MemCheck.Application.Searching;
using MemCheck.Application.Users;
using MemCheck.Database;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Functions.Worker;
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
    protected override async Task<RunResult> RunAndCreateReportMailMainPartAsync(string defaultMailSubject)
    {
        var callContext = NewCallContext();

        var result = new StringBuilder();
        var replacer = new ReplaceTextInAllVisibleCards(callContext);

        var wikipediaRequest = new ReplaceTextInAllVisibleCards.Request(BotUserId, "https://fr.m.wikipedia.org/wiki/", "https://fr.wikipedia.org/wiki/", "Utilisation de liens Wikipédia par défaut au lieu de la version mobile");
        var wikipediaResult = await replacer.RunAsync(wikipediaRequest);
        result.Append(GetMailBody(wikipediaResult.ChangedCardGuids, "Wikipédia"));

        var wiktionaryRequest = new ReplaceTextInAllVisibleCards.Request(BotUserId, "https://fr.m.wiktionary.org/wiki/", "https://fr.wiktionary.org/wiki/", "Utilisation de liens Wiktionnaire par défaut au lieu de la version mobile");
        var wiktionaryResult = await replacer.RunAsync(wiktionaryRequest);
        result.Append(GetMailBody(wiktionaryResult.ChangedCardGuids, "Wiktionnaire"));

        var changeCount = wikipediaResult.ChangedCardGuids.Length + wiktionaryResult.ChangedCardGuids.Length;

        var changeInfo = changeCount switch
        {
            0 => "no change",
            1 => "1 change",
            _ => $"{changeCount} changes",
        };

        var mailSubject = $"{defaultMailSubject} ({changeInfo})";

        return new RunResult(mailSubject, result);
    }
    #endregion
    public MakeWikipediaLinksDesktop(TelemetryConfiguration telemetryConfiguration, MemCheckDbContext memCheckDbContext, MemCheckUserManager userManager, ILogger<MakeWikipediaLinksDesktop> logger)
        : base(telemetryConfiguration, memCheckDbContext, userManager, logger)
    {
    }
    [Function(nameof(MakeWikipediaLinksDesktop))]
    public async Task Run([TimerTrigger(Constants.Cron_MakeWikipediaLinksDesktop)] TimerInfo timer, FunctionContext context)
    {
        await RunAsync(timer, context);
    }
}
