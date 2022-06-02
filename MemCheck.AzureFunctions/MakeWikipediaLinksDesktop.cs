using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MemCheck.Application.Cards;
using MemCheck.Application.Searching;
using MemCheck.Application.Users;
using MemCheck.Database;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.WebJobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MemCheck.AzureFunctions;

// Working on a phone, the adding of hyperlinks is generally made with the mobile version of Wikipedia.
// It makes much more sense to use the desktop version, since the site will automatically switch to the mobile version if suitable.
// Example of mobile link: "https://fr.m.wikipedia.org/wiki/Barbotin", will be replaced with "https://fr.wikipedia.org/wiki/Barbotin"
// Similarly, "https://fr.m.wiktionary.org/wiki/memoire" will be replaced with "https://fr.wiktionary.org/wiki/memoire"

public sealed class MakeWikipediaLinksDesktop : AbstractMemCheckAzureFunction
{
    #region Private methods
    private static string GetMailBody(ImmutableArray<Guid> changedCardGuids)
    {
        var writer = new StringBuilder()
            .Append(CultureInfo.InvariantCulture, $"<h1>{changedCardGuids.Length} cards changed for Wikipedia links</h1>")
            .Append("<ul>");

        foreach (var changedCard in changedCardGuids)
            writer = writer.Append(CultureInfo.InvariantCulture, $"<li>https://memcheckfr.azurewebsites.net/Authoring?CardId={changedCard}</li>");

        writer = writer.Append("</ul>");

        return writer.ToString();
    }
    #endregion
    #region Protected override methods
    protected override async Task<string> RunAndCreateReportMailMainPartAsync()
    {
        var callContext = NewCallContext();

        var wikipediaRequest = new ReplaceTextInAllVisibleCards.Request(BotUserId, "https://fr.m.wikipedia.org/wiki/", "https://fr.wikipedia.org/wiki/", "Utilisation de liens Wikipédia par défaut au lieu de la version mobile");
        var replacer = new ReplaceTextInAllVisibleCards(callContext);
        var wikipediaResult = await replacer.RunAsync(wikipediaRequest);

        return GetMailBody(wikipediaResult.ChangedCardGuids);
    }
    #endregion
    public MakeWikipediaLinksDesktop(TelemetryConfiguration telemetryConfiguration, MemCheckDbContext memCheckDbContext, MemCheckUserManager userManager, ILogger<SendStatsToAdministrators> logger)
        : base(telemetryConfiguration, memCheckDbContext, userManager, logger)
    {
    }
    [FunctionName(nameof(MakeWikipediaLinksDesktop))]
    public async Task Run([TimerTrigger(Constants.CronAt5Daily)] TimerInfo timer, ExecutionContext context)
    {
        await RunAsync(timer, context);
    }
}

