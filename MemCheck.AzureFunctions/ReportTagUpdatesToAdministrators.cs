using System;
using System.Text;
using System.Threading.Tasks;
using MemCheck.Application.Tags;
using MemCheck.Application.Users;
using MemCheck.Database;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace MemCheck.AzureFunctions;

public sealed class ReportTagUpdatesToAdministrators : AbstractMemCheckAzureFunction
{
    #region Private methods
    private static string GetTagMailPart(GetModifiedTags.ResultTag tag)
    {
        var writer = new StringBuilder();
        writer.AppendHtmlHeader(2, tag.Versions[0].TagName);
        using (writer.HtmlParagraph())
        {
            writer.AppendHtmlText($"<strong>Id:</strong> {tag.TagId}", true);
            writer.AppendHtmlText($"<strong>Count of public cards:</strong> {tag.CountOfPublicCards}", true);
            writer.AppendHtmlText($"<strong>Average rating of public cards:</strong> {tag.AverageRatingOfPublicCards}", true);
        }
        foreach (var tagVersion in tag.Versions)
            using (writer.HtmlLi())
            {
                writer.AppendHtmlText($"<strong>Tag version on {tagVersion.UtcDate}: {tagVersion.VersionType}</strong>", true);
                writer.AppendHtmlText($"<strong>Tag name:</strong> {tagVersion.TagName}", true);
                writer.AppendHtmlText($"<strong>Created by</strong> {tagVersion.CreatorName}", true);
                writer.AppendHtmlText($"<strong>Description:</strong> {tagVersion.Description}", true);
                writer.AppendHtmlText($"<strong>Version description:</strong> {tagVersion.VersionDescription}", true);
            }

        return writer.ToString();
    }
    #endregion
    #region Protected override methods
    protected override async Task<RunResult> RunAndCreateReportMailMainPartAsync(string defaultMailSubject)
    {
        var request = new GetModifiedTags.Request(DateTime.UtcNow.Subtract(TimeSpan.FromDays(3))); // We could have chosen to report only changes since the day before, but this is in case a day is missed for some reason (either reading the mail or running the function)
        var runner = new GetModifiedTags(NewCallContext());
        var result = await runner.RunAsync(request);

        var body = new StringBuilder();
        body.AppendHtmlHeader(1, $"{result.Tags.Length} tags changed since {request.SinceUtcDate}");
        foreach (var tag in result.Tags)
            body.Append(GetTagMailPart(tag));
        return new RunResult(defaultMailSubject, body);
    }
    #endregion
    public ReportTagUpdatesToAdministrators(TelemetryConfiguration telemetryConfiguration, MemCheckDbContext memCheckDbContext, MemCheckUserManager userManager, ILogger<SendStatsToAdministrators> logger)
        : base(telemetryConfiguration, memCheckDbContext, userManager, logger)
    {
    }
    [Function(nameof(ReportTagUpdatesToAdministrators))]
    public async Task Run([TimerTrigger(Constants.Cron_ReportTagUpdatesToAdministrators)] TimerInfo timer, FunctionContext context)
    {
        await RunAsync(timer, context);
    }
}
