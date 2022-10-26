using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using MemCheck.Application.Users;
using MemCheck.Database;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace MemCheck.AzureFunctions;

public sealed class RefreshAverageRatings : AbstractMemCheckAzureFunction
{
    public RefreshAverageRatings(TelemetryConfiguration telemetryConfiguration, MemCheckDbContext memCheckDbContext, MemCheckUserManager userManager, ILogger<UpdateTagStats> logger)
        : base(telemetryConfiguration, memCheckDbContext, userManager, logger)
    {
    }
    [FunctionName(nameof(RefreshAverageRatings))]
    public async Task Run([TimerTrigger(Constants.Cron_RefreshAverageRatings)] TimerInfo timer, ExecutionContext context)
    {
        await RunAsync(timer, context);
    }
    protected override async Task<RunResult> RunAndCreateReportMailMainPartAsync(string defaultMailSubject)
    {
        var refresher = new Application.Ratings.RefreshAverageRatings(NewCallContext());
        var result = await refresher.RunAsync(new Application.Ratings.RefreshAverageRatings.Request());

        var reportMailMainPart = new StringBuilder()
            .Append("<p>")
            .Append(CultureInfo.InvariantCulture, $"<li>Considered {result.TotalCardCountInDb} cards</li>")
            .Append(CultureInfo.InvariantCulture, $"<li>Changed the average ratings for {result.ChangedAverageRatingCount} cards</li>")
            .Append("</p>");

        var changeInfo = result.ChangedAverageRatingCount switch
        {
            0 => "no change",
            1 => "1 change",
            _ => $"{result.ChangedAverageRatingCount} changes",
        };

        var mailSubject = $"{defaultMailSubject} ({changeInfo})";

        return new RunResult(mailSubject, reportMailMainPart);
    }
}
