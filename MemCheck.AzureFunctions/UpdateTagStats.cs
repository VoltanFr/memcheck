using System.Text;
using System.Threading.Tasks;
using MemCheck.Application.Tags;
using MemCheck.Application.Users;
using MemCheck.Database;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace MemCheck.AzureFunctions;

internal sealed class UpdateTagStats : AbstractMemCheckAzureFunction
{
    #region Fields
    private const string FuncName = nameof(UpdateTagStats);
    #endregion
    public UpdateTagStats(TelemetryConfiguration telemetryConfiguration, MemCheckDbContext memCheckDbContext, MemCheckUserManager userManager, ILogger<SendStatsToAdministrators> logger)
        : base(telemetryConfiguration, memCheckDbContext, userManager, logger)
    {
    }
    [FunctionName(FuncName)]
    public async Task Run([TimerTrigger(
        Constants.CronAt5Daily
        #if DEBUG
        , RunOnStartup = true
        #endif
        )] TimerInfo myTimer, ExecutionContext context)
    {
        await RunAsync();
    }
    protected override string FunctionName => FuncName;
    protected async override Task DoRunAsync()
    {
        var updater = new RefreshTagStats(NewCallContext());
        var result = await updater.RunAsync(new RefreshTagStats.Request());

        var mailBody = new StringBuilder();
        mailBody.Append("<style>");
        mailBody.Append("thead{background-color:darkgray;color:white;}");
        mailBody.Append("table{border-width:1px;border-color:green;border-collapse:collapse;}");
        mailBody.Append("tr{border-width:1px;border-style:solid;border-color:black;}");
        mailBody.Append("td{border-width:1px;border-style:solid;border-color:darkgray;}");
        mailBody.Append("tr:nth-child(even){background-color:lightgray;}");
        mailBody.Append("</style>");
        mailBody.Append($"<h1>{result.Tags.Length} MemCheck tags</h1>");
        mailBody.Append("<p><table>");
        mailBody.Append("<thead><tr><th>Name</th><th>Previous count</th><th>Previous average</th><th>New count</th><th>New average</th></tr></thead>");
        mailBody.Append("<body>");
        foreach (var tag in result.Tags)
        {
            mailBody.Append("<tr style='nth-child(odd) {background: lightgray}'>");
            mailBody.Append($"<td>{tag.TagName}</td>");
            var cardCountChanged = tag.CardCountBeforeRun != tag.CardCountAfterRun;
            var averageChanged = tag.AverageRatingBeforeRun != tag.AverageRatingAfterRun;
            mailBody.Append("<td>");
            if (cardCountChanged)
                mailBody.Append("<strong>");
            mailBody.Append($"{tag.CardCountBeforeRun}");
            if (cardCountChanged)
                mailBody.Append("</strong>");
            mailBody.Append("</td>");
            mailBody.Append("<td>");
            if (averageChanged)
                mailBody.Append("<strong>");
            mailBody.Append($"{tag.AverageRatingBeforeRun:0.##}");
            if (averageChanged)
                mailBody.Append("</strong>");
            mailBody.Append("</td>");
            mailBody.Append("<td>");
            if (cardCountChanged)
                mailBody.Append("<strong>");
            mailBody.Append($"{tag.CardCountAfterRun}");
            if (cardCountChanged)
                mailBody.Append("</strong>");
            mailBody.Append("</td>");
            mailBody.Append("<td>");
            if (averageChanged)
                mailBody.Append("<strong>");
            mailBody.Append($"{tag.AverageRatingAfterRun:0.##}");
            if (averageChanged)
                mailBody.Append("</strong>");
            mailBody.Append("</td>");
            mailBody.Append("</tr>");
        }
        mailBody.Append("</body>");
        mailBody.Append("</table></p>");
        mailBody.Append(MailSender.GetMailFooter(FunctionName, StartTime, await AdminsAsync()));

        await MailSender.SendAsync("MemCheck tags refreshed", mailBody.ToString(), await AdminsAsync());
    }
}
