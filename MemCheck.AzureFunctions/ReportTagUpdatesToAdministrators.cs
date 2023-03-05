using System;
using System.Text;
using System.Threading.Tasks;
using MemCheck.Application.Tags;
using MemCheck.Application.Users;
using MemCheck.Basics;
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
            writer.AppendHtmlText($"<strong>Average rating of public cards:</strong> {tag.AverageRatingOfPublicCards:F2}", true);
        }
        using (writer.HtmlUl())
            for (var versionIndex = 0; versionIndex < tag.Versions.Length; versionIndex++)
            {
                var tagVersion = tag.Versions[versionIndex];
                var tagNextVersion = versionIndex + 1 < tag.Versions.Length ? tag.Versions[versionIndex + 1] : tag.Versions[versionIndex];

                using (writer.HtmlLi())
                {
                    {
                        var versionType = tagVersion.VersionType.ToString();
                        if (tagNextVersion != null && tagVersion.VersionType != tagNextVersion.VersionType)
                            versionType = $"<i>{tagVersion.VersionType}</i>";
                        writer.AppendHtmlText($"<strong>Tag version on {DateServices.AsIsoWithHHmm(tagVersion.UtcDate)}: {versionType}</strong>", true);
                    }
                    {
                        var tagName = tagVersion.TagName;
                        if (tagNextVersion != null && tagVersion.TagName != tagNextVersion.TagName)
                            tagName = $"<i>{tagVersion.TagName}</i>";
                        writer.AppendHtmlText($"<strong>Tag name:</strong> {tagName}", true);
                    }
                    {
                        var creatorName = tagVersion.CreatorName;
                        if (tagNextVersion != null && tagVersion.CreatorName != tagNextVersion.CreatorName)
                            creatorName = $"<i>{tagVersion.CreatorName}</i>";
                        writer.AppendHtmlText($"<strong>Created by</strong> {creatorName}", true);
                    }
                    {
                        var description = tagVersion.Description;
                        if (tagNextVersion != null && tagVersion.Description != tagNextVersion.Description)
                            description = $"<i>{tagVersion.Description}</i>";
                        writer.AppendHtmlText($"<strong>Description:</strong> {description}", true);
                    }
                    {
                        var versionDescription = tagVersion.VersionDescription;
                        if (tagNextVersion != null && tagVersion.VersionDescription != tagNextVersion.VersionDescription)
                            versionDescription = $"<i>{tagVersion.VersionDescription}</i>";
                        writer.AppendHtmlText($"<strong>Version description:</strong> {versionDescription}", true);
                    }
                }
            }
        return writer.ToString();
    }
    #endregion
    #region Protected override methods
    protected override async Task<RunResult> RunAndCreateReportMailMainPartAsync(string defaultMailSubject)
    {
        const int dayCount = 3;
        var request = new GetModifiedTags.Request(DateTime.UtcNow.Subtract(TimeSpan.FromDays(dayCount)).Date); // We could have chosen to report only changes since the day before, but this is in case a day is missed for some reason (either reading the mail or running the function)
        var runner = new GetModifiedTags(NewCallContext());
        var result = await runner.RunAsync(request);

        var changeInfo = result.Tags.Length switch
        {
            0 => "no tag change",
            1 => "1 tag change",
            _ => $"{result.Tags.Length} tag changes",
        };

        var body = new StringBuilder();
        body.AppendHtmlHeader(1, $"{changeInfo} in the last {dayCount} days");
        foreach (var tag in result.Tags)
            body.Append(GetTagMailPart(tag));

        return new RunResult($"Mnesios: {changeInfo}", body);

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
