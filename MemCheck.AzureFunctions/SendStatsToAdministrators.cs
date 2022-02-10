using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MemCheck.Application;
using MemCheck.Application.QueryValidation;
using MemCheck.Application.Users;
using MemCheck.Database;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace MemCheck.AzureFunctions;

public sealed class SendStatsToAdministrators
{
    #region Fields
    private const string FunctionName = nameof(SendStatsToAdministrators);
    private readonly TelemetryClient telemetryClient;
    private readonly MemCheckDbContext memCheckDbContext;
    private readonly MemCheckUserManager userManager;
    private readonly ILogger logger;
    private readonly IRoleChecker roleChecker;
    private readonly Guid runningUserId;
    private readonly DateTime startTime;
    private readonly MailSender mailSender;
    #endregion
    #region Private methods
    private async Task<ImmutableList<EmailAddress>> GetAdminsAsync()
    {
        var getter = new GetAdminEmailAddesses(new Application.CallContext(memCheckDbContext, new MemCheckTelemetryClient(telemetryClient), new FakeStringLocalizer(), roleChecker));
        var getterResult = await getter.RunAsync(new GetAdminEmailAddesses.Request(runningUserId));
        var result = getterResult.Users;
        return result.Select(address => new EmailAddress(address.Email, address.Name)).ToImmutableList();
    }
    private async Task<ImmutableList<GetAllUsers.ResultUserModel>> GetAllUsersAsync()
    {
        var getter = new GetAllUsers(new Application.CallContext(memCheckDbContext, new MemCheckTelemetryClient(telemetryClient), new FakeStringLocalizer(), roleChecker));
        var page = 1;
        var result = new List<GetAllUsers.ResultUserModel>();
        while (true)
        {
            var getterResult = await getter.RunAsync(new GetAllUsers.Request(runningUserId, 2, page, ""));
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
    {
        telemetryClient = new TelemetryClient(telemetryConfiguration);
        this.memCheckDbContext = memCheckDbContext;
        this.userManager = userManager;
        this.logger = logger;
        roleChecker = new ProdRoleChecker(userManager); ;
        runningUserId = new Guid(Environment.GetEnvironmentVariable("RunningUserId"));
        startTime = DateTime.UtcNow;
        mailSender = new MailSender(FunctionName, startTime, logger);
    }
    [FunctionName(FunctionName)]
    public async Task Run([TimerTrigger(
        Constants.CronEvery5Min
        #if DEBUG
        , RunOnStartup = true
        #endif
        )] TimerInfo myTimer, ExecutionContext context)
    {
        try
        {
            logger.LogInformation($"{FunctionName} Azure func starting at {DateTime.Now} on {Environment.MachineName}");
            telemetryClient.TrackEvent($"{FunctionName} Azure func start");

            var admins = await GetAdminsAsync();
            var allUsers = await GetAllUsersAsync();
            var mailBody = new StatsToAdminMailBuilder(FunctionName, startTime, admins, allUsers).GetMailBody();

            await mailSender.SendAsync("MemCheck stats", mailBody, admins);
        }
        catch (Exception ex)
        {
            await mailSender.SendFailureInfoMailAsync(ex);
        }
        finally
        {
            logger.LogInformation($"Function '{FunctionName}' ending, {DateTime.Now}");
        }
    }
}
