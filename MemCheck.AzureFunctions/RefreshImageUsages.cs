using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using MemCheck.Application.Images;
using MemCheck.Application.Users;
using MemCheck.Database;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace MemCheck.AzureFunctions;

public sealed class RefreshImageUsages : AbstractMemCheckAzureFunction
{
    public RefreshImageUsages(TelemetryConfiguration telemetryConfiguration, MemCheckDbContext memCheckDbContext, MemCheckUserManager userManager, ILogger<UpdateTagStats> logger)
        : base(telemetryConfiguration, memCheckDbContext, userManager, logger)
    {
    }
    [FunctionName(nameof(RefreshImageUsages))]
    public async Task Run([TimerTrigger(Constants.Cron_RefreshImageUsages)] TimerInfo timer, ExecutionContext context)
    {
        await RunAsync(timer, context);
    }
    protected override async Task<RunResult> RunAndCreateReportMailMainPartAsync(string defaultMailSubject)
    {
        var updater = new RefreshImagesInCards(NewCallContext());
        var result = await updater.RunAsync(new RefreshImagesInCards.Request());

        var reportMailMainPart = new StringBuilder()
            .Append(CultureInfo.InvariantCulture, $"<h1>Refresh image usages table</h1>")
            .Append("<p><ul>")
            .Append(CultureInfo.InvariantCulture, $"<li>{result.TotalImageCount} images in DB</li>")
            .Append(CultureInfo.InvariantCulture, $"<li>{result.TotalCardCount} cards in DB</li>")
            .Append(CultureInfo.InvariantCulture, $"<li>{result.ImagesInCardsCountOnStart} image usages on start</li>")
            .Append(CultureInfo.InvariantCulture, $"<li>{result.ImagesInCardsCountOnEnd} image usages on end</li>")
            .Append(CultureInfo.InvariantCulture, $"<li>{result.ChangeCount} total changes</li>")
            .Append("</ul></p>");

        var changeInfo = result.ChangeCount switch
        {
            0 => "no change",
            1 => "1 change",
            _ => $"{result.ChangeCount} changes",
        };

        var mailSubject = $"{defaultMailSubject} ({changeInfo})";

        return new RunResult(mailSubject, reportMailMainPart);
    }
}
