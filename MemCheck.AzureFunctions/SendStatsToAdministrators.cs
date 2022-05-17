using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
    protected override string FunctionName => FuncName;
    protected override async Task<string> RunAndCreateReportMailMainPartAsync()
    {
        var allUsers = await GetAllUsersAsync();
        var recentDemos = await GetRecentDemosAsync();
        var tagNames = GetTagNames();
        return new StatsToAdminMailBuilder(allUsers, recentDemos, tagNames).GetMailBody();
    }
}
