using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using MemCheck.Application.Tags;
using MemCheck.Application.Users;
using MemCheck.Database;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace MemCheck.AzureFunctions;

public sealed class UpdateTagStats : AbstractMemCheckAzureFunction
{
    public UpdateTagStats(TelemetryConfiguration telemetryConfiguration, MemCheckDbContext memCheckDbContext, MemCheckUserManager userManager, ILogger<UpdateTagStats> logger)
        : base(telemetryConfiguration, memCheckDbContext, userManager, logger)
    {
    }
    [FunctionName(nameof(UpdateTagStats))]
    public async Task Run([TimerTrigger(Constants.Cron_UpdateTagStats)] TimerInfo timer, ExecutionContext context)
    {
        await RunAsync(timer, context);
    }
    protected override async Task<string> RunAndCreateReportMailMainPartAsync()
    {
        var updater = new RefreshTagStats(NewCallContext());
        var result = await updater.RunAsync(new RefreshTagStats.Request());

        var reportMailMainPart = new StringBuilder()
            .Append(CultureInfo.InvariantCulture, $"<h1>{result.Tags.Length} MemCheck tags</h1>")
            .Append("<p><table>")
            .Append("<thead><tr><th>Name</th><th>Previous count</th><th>Previous average</th><th>New count</th><th>New average</th></tr></thead>");

        foreach (var tag in result.Tags)
        {
            reportMailMainPart = reportMailMainPart
                .Append("<tr>")
                .Append(CultureInfo.InvariantCulture, $"<td>{tag.TagName}</td>");
            var cardCountChanged = tag.CardCountBeforeRun != tag.CardCountAfterRun;
            var averageChanged = tag.AverageRatingBeforeRun != tag.AverageRatingAfterRun;
            reportMailMainPart = reportMailMainPart.Append("<td>");
            if (cardCountChanged)
                reportMailMainPart = reportMailMainPart.Append("<strong>");
            reportMailMainPart = reportMailMainPart.Append(CultureInfo.InvariantCulture, $"{tag.CardCountBeforeRun}");
            if (cardCountChanged)
                reportMailMainPart = reportMailMainPart.Append("</strong>");
            reportMailMainPart = reportMailMainPart
                .Append("</td>")
                .Append("<td>");
            if (averageChanged)
                reportMailMainPart = reportMailMainPart.Append("<strong>");
            reportMailMainPart = reportMailMainPart.Append(CultureInfo.InvariantCulture, $"{tag.AverageRatingBeforeRun:0.##}");
            if (averageChanged)
                reportMailMainPart = reportMailMainPart.Append("</strong>");
            reportMailMainPart = reportMailMainPart
                .Append("</td>")
                .Append("<td>");
            if (cardCountChanged)
                reportMailMainPart = reportMailMainPart.Append("<strong>");
            reportMailMainPart = reportMailMainPart.Append(CultureInfo.InvariantCulture, $"{tag.CardCountAfterRun}");
            if (cardCountChanged)
                reportMailMainPart = reportMailMainPart.Append("</strong>");
            reportMailMainPart = reportMailMainPart
                .Append("</td>")
                .Append("<td>");
            if (averageChanged)
                reportMailMainPart = reportMailMainPart.Append("<strong>");
            reportMailMainPart = reportMailMainPart.Append(CultureInfo.InvariantCulture, $"{tag.AverageRatingAfterRun:0.##}");
            if (averageChanged)
                reportMailMainPart = reportMailMainPart.Append("</strong>");
            reportMailMainPart = reportMailMainPart
                .Append("</td>")
                .Append("</tr>");
        }

        reportMailMainPart = reportMailMainPart.Append("</table></p>");
        return reportMailMainPart.ToString();
    }
}
