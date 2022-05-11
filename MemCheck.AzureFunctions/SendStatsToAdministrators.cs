using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using MemCheck.Application.Users;
using MemCheck.Database;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace MemCheck.AzureFunctions;

public sealed class SendStatsToAdministrators : AbstractMemCheckAzureFunction
{
    #region Fields
    private const string FuncName = nameof(SendStatsToAdministrators);
    #endregion
    #region Private methods
    private async Task<ImmutableList<GetAllUsers.ResultUserModel>> GetAllUsersAsync()
    {
        var getter = new GetAllUsers(NewCallContext());
        var page = 1;
        var result = new List<GetAllUsers.ResultUserModel>();
        while (true)
        {
            var getterResult = await getter.RunAsync(new GetAllUsers.Request(RunningUserId, 50, page, ""));
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
    #endregion
    public SendStatsToAdministrators(TelemetryConfiguration telemetryConfiguration, MemCheckDbContext memCheckDbContext, MemCheckUserManager userManager, ILogger<SendStatsToAdministrators> logger)
        : base(telemetryConfiguration, memCheckDbContext, userManager, logger)
    {
    }
    [FunctionName(FuncName)]
    public async Task Run([TimerTrigger(
        Constants.CronEach5Min
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
        var allUsers = await GetAllUsersAsync();
        var mailBody = new StatsToAdminMailBuilder(FunctionName, StartTime, await AdminsAsync(), allUsers).GetMailBody();
        await MailSender.SendAsync("MemCheck stats", mailBody, await AdminsAsync());
    }
}
