using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MemCheck.Application.Cards;
using MemCheck.Application.Users;
using MemCheck.Database;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.WebJobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MemCheck.AzureFunctions;

public sealed class SendStatsToAdministrators : AbstractMemCheckAzureFunction
{
    #region Fields
    private const string FuncName = nameof(SendStatsToAdministrators);
    #endregion
    #region Private methods
    private async Task<ImmutableList<GetAllUsersStats.ResultUserModel>> GetAllUsersAsync()
    {
        var getter = new GetAllUsersStats(NewCallContext());
        var page = 1;
        var result = new List<GetAllUsersStats.ResultUserModel>();
        while (true)
        {
            var getterResult = await getter.RunAsync(new GetAllUsersStats.Request(RunningUserId, 50, page, ""));
            if (getterResult.Users.Any())
            {
                result.AddRange(getterResult.Users);
                page++;
            }
            else
                break;
        }
        return result.ToImmutableList();
    }
    private async Task<GetRecentDemoUses.Result> GetRecentDemosAsync()
    {
        var getter = new GetRecentDemoUses(NewCallContext());
        var queryResult = await getter.RunAsync(new GetRecentDemoUses.Request(30));
        return queryResult;
    }
    private ImmutableDictionary<Guid, string> GetTagNames()
    {
        return NewCallContext().DbContext.Tags.AsNoTracking().Select(t => new { t.Id, t.Name }).ToImmutableDictionary(t => t.Id, t => t.Name);
    }
    private static string GetUsersPart(ImmutableList<GetAllUsersStats.ResultUserModel> allUsers)
    {
        var writer = new StringBuilder();

        writer.Append(CultureInfo.InvariantCulture, $"<h1>{allUsers.Count} Users</h1>")
            .Append("<p><table>")
            .Append(@"<thead><tr><th scope=""col"">Name</th><th scope=""col"">Last seen</th><th scope=""col"">Registration</th><th scope=""col"">Decks</th></tr></thead>")
            .Append("<tbody>");

        foreach (var user in allUsers)
        {
            writer = writer
                .Append("<tr>")
                .Append(CultureInfo.InvariantCulture, $"<td>{user.UserName}</td>")
                .Append(CultureInfo.InvariantCulture, $"<td>{user.LastSeenUtcDate}</td>")
                .Append(CultureInfo.InvariantCulture, $"<td>{user.RegistrationUtcDate}</td>")
                .Append("<td><ul>");

            foreach (var deck in user.Decks)
                writer = writer.Append(CultureInfo.InvariantCulture, $"<li>{deck.Name}: {deck.CardCount} cards</li>");

            writer = writer
                .Append("</ul></td></tr>");
        }

        writer = writer
            .Append("</tbody>")
            .Append("</table></p>");

        return writer.ToString();
    }
    private static string GetRecentDemosPart(GetRecentDemoUses.Result recentDemos, ImmutableDictionary<Guid, string> tagNames)
    {
        var writer = new StringBuilder();

        writer.Append(CultureInfo.InvariantCulture, $"<h1>{recentDemos.Entries.Length} demos in the last {recentDemos.DayCount} days</h1>")
            .Append("<p><table>")
            .Append("<thead><tr><th>Date</th><th>Tag</th><th>Card count</th></tr></thead>")
            .Append("<tbody>");

        foreach (var recentDemo in recentDemos.Entries)
        {
            writer = writer
                .Append("<tr>")
                .Append(CultureInfo.InvariantCulture, $"<td>{recentDemo.DownloadUtcDate}</td><td>{tagNames[recentDemo.TagId]}</td><td>{recentDemo.CountOfCardsReturned}</td>")
                .Append("</tr>");
        }

        writer = writer
            .Append("</tbody>")
            .Append("</table></p>");

        return writer.ToString();
    }
    private static string GetMailBody(ImmutableList<GetAllUsersStats.ResultUserModel> allUsers, GetRecentDemoUses.Result recentDemos, ImmutableDictionary<Guid, string> tagNames)
    {
        return GetUsersPart(allUsers) + GetRecentDemosPart(recentDemos, tagNames);
    }
    #endregion
    #region Protected override methods
    protected override string FunctionName => FuncName;
    protected override async Task<string> RunAndCreateReportMailMainPartAsync()
    {
        var allUsers = await GetAllUsersAsync();
        var recentDemos = await GetRecentDemosAsync();
        var tagNames = GetTagNames();
        return GetMailBody(allUsers, recentDemos, tagNames);
    }
    #endregion
    public SendStatsToAdministrators(TelemetryConfiguration telemetryConfiguration, MemCheckDbContext memCheckDbContext, MemCheckUserManager userManager, ILogger<SendStatsToAdministrators> logger)
        : base(telemetryConfiguration, memCheckDbContext, userManager, logger)
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
}
