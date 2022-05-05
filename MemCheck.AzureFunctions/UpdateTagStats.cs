using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using MemCheck.Application.Tags;
using MemCheck.Database;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace MemCheck.AzureFunctions;

public sealed class UpdateTagStats : AbstractMemCheckAzureFunction
{
    #region Fields
    private const string FuncName = nameof(UpdateTagStats);
    #endregion
    public UpdateTagStats(TelemetryConfiguration telemetryConfiguration, MemCheckDbContext memCheckDbContext, ILogger<UpdateTagStats> logger)
        : base(telemetryConfiguration, memCheckDbContext, logger)
    {
    }
    [FunctionName(FuncName)]
    public async Task Run([TimerTrigger(
        Constants.CronAt2Daily
        #if DEBUG
        , RunOnStartup = true
        #endif
        )] TimerInfo myTimer, ExecutionContext context)
    {
        await RunAsync();
    }
    protected override string FunctionName => FuncName;
    protected override async Task DoRunAsync()
    {
        var updater = new RefreshTagStats(NewCallContext());
        var result = await updater.RunAsync(new RefreshTagStats.Request());

        var mailBody = new StringBuilder()
            .Append("<style>")
            .Append("thead{background-color:darkgray;color:white;}")
            .Append("table{border-width:1px;border-color:green;border-collapse:collapse;}")
            .Append("tr{border-width:1px;border-style:solid;border-color:black;}")
            .Append("td{border-width:1px;border-style:solid;border-color:darkgray;}")
            .Append("tr:nth-child(even){background-color:lightgray;}")
            .Append("</style>")
            .Append(CultureInfo.InvariantCulture, $"<h1>{result.Tags.Length} MemCheck tags</h1>")
            .Append("<p><table>")
            .Append("<thead><tr><th>Name</th><th>Previous count</th><th>Previous average</th><th>New count</th><th>New average</th></tr></thead>")
            .Append("<body>");
        foreach (var tag in result.Tags)
        {
            mailBody = mailBody
                .Append("<tr style='nth-child(odd) {background: lightgray}'>")
                .Append(CultureInfo.InvariantCulture, $"<td>{tag.TagName}</td>");
            var cardCountChanged = tag.CardCountBeforeRun != tag.CardCountAfterRun;
            var averageChanged = tag.AverageRatingBeforeRun != tag.AverageRatingAfterRun;
            mailBody = mailBody.Append("<td>");
            if (cardCountChanged)
                mailBody = mailBody.Append("<strong>");
            mailBody = mailBody.Append(CultureInfo.InvariantCulture, $"{tag.CardCountBeforeRun}");
            if (cardCountChanged)
                mailBody = mailBody.Append("</strong>");
            mailBody = mailBody
                .Append("</td>")
                .Append("<td>");
            if (averageChanged)
                mailBody = mailBody.Append("<strong>");
            mailBody = mailBody.Append(CultureInfo.InvariantCulture, $"{tag.AverageRatingBeforeRun:0.##}");
            if (averageChanged)
                mailBody = mailBody.Append("</strong>");
            mailBody = mailBody
                .Append("</td>")
                .Append("<td>");
            if (cardCountChanged)
                mailBody = mailBody.Append("<strong>");
            mailBody = mailBody.Append(CultureInfo.InvariantCulture, $"{tag.CardCountAfterRun}");
            if (cardCountChanged)
                mailBody = mailBody.Append("</strong>");
            mailBody = mailBody
                .Append("</td>")
                .Append("<td>");
            if (averageChanged)
                mailBody = mailBody.Append("<strong>");
            mailBody = mailBody.Append(CultureInfo.InvariantCulture, $"{tag.AverageRatingAfterRun:0.##}");
            if (averageChanged)
                mailBody = mailBody.Append("</strong>");
            mailBody = mailBody
                .Append("</td>")
                .Append("</tr>");
        }
        mailBody = mailBody
            .Append("</body>")
            .Append("</table></p>")
            .Append(MailSender.GetMailFooter(FunctionName, StartTime, await AdminsAsync()));

        await MailSender.SendAsync("MemCheck tags refreshed", mailBody.ToString(), await AdminsAsync());
    }
}
