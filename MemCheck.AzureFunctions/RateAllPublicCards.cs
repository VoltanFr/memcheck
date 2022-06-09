using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using MemCheck.Application.Users;
using MemCheck.Database;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace MemCheck.AzureFunctions;

public sealed class RateAllPublicCards : AbstractMemCheckAzureFunction
{
    public RateAllPublicCards(TelemetryConfiguration telemetryConfiguration, MemCheckDbContext memCheckDbContext, MemCheckUserManager userManager, ILogger<UpdateTagStats> logger)
        : base(telemetryConfiguration, memCheckDbContext, userManager, logger)
    {
    }
    [FunctionName(nameof(RateAllPublicCards))]
    public async Task Run([TimerTrigger(Constants.Cron_RateAllPublicCards)] TimerInfo timer, ExecutionContext context)
    {
        await RunAsync(timer, context);
    }
    protected override async Task<string> RunAndCreateReportMailMainPartAsync()
    {
        var rater = new Application.Ratings.RateAllPublicCards(NewCallContext());
        var result = await rater.RunAsync(new Application.Ratings.RateAllPublicCards.Request(BotUserId));

        var reportMailMainPart = new StringBuilder()
            .Append("<p>")
            .Append(CultureInfo.InvariantCulture, $"<li>Considered {result.PublicCardCount} public cards</li>")
            .Append(CultureInfo.InvariantCulture, $"<li>Changed the bot's ratings for {result.ChangedRatingCount} cards</li>")
            .Append("</p>");

        return reportMailMainPart.ToString();
    }
}
