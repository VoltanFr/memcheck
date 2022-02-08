using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MemCheck.Application;
using MemCheck.Application.QueryValidation;
using MemCheck.Application.Users;
using MemCheck.Basics;
using MemCheck.Database;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace MemCheck.AzureFunctions;

public class SendStatsToAdministrators
{
    #region Fields
    private const string FunctionName = nameof(SendStatsToAdministrators);
    private readonly TelemetryClient telemetryClient;
    private readonly MemCheckDbContext memCheckDbContext;
    private readonly MemCheckUserManager userManager;
    private readonly IRoleChecker roleChecker;
    private readonly Guid runningUserId;
    private readonly DateTime startTime;
    #endregion
    #region Private methods
    private SendGridClient GetSendGridClient()
    {
        var sendGridKey = Environment.GetEnvironmentVariable("SendGridKey");
        return new SendGridClient(sendGridKey);
    }
    private EmailAddress GetSenderEmail()
    {
        var sendGridSender = Environment.GetEnvironmentVariable("SendGridSender");
        var sendGridUser = Environment.GetEnvironmentVariable("SendGridUser");
        return new EmailAddress(sendGridSender, sendGridUser);

    }
    private async Task<ImmutableList<GetAdminEmailAddesses.ResultUserModel>> GetAdminsAsync()
    {
        var getter = new GetAdminEmailAddesses(new Application.CallContext(memCheckDbContext, new MemCheckTelemetryClient(telemetryClient), new FakeStringLocalizer(), roleChecker));
        var getterResult = await getter.RunAsync(new GetAdminEmailAddesses.Request(runningUserId));
        return getterResult.Users.ToImmutableList();
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
    public SendStatsToAdministrators(TelemetryConfiguration telemetryConfiguration, MemCheckDbContext memCheckDbContext, MemCheckUserManager userManager)
    {
        telemetryClient = new TelemetryClient(telemetryConfiguration);
        this.memCheckDbContext = memCheckDbContext;
        this.userManager = userManager;
        roleChecker = new ProdRoleChecker(userManager); ;
        runningUserId = new Guid(Environment.GetEnvironmentVariable("RunningUserId"));
        startTime = DateTime.UtcNow;
    }
    [FunctionName(FunctionName)]
    public async Task Run([TimerTrigger("0 */8 * * *"
        #if DEBUG
        , RunOnStartup = true
        #endif
        )] TimerInfo myTimer, ExecutionContext context, ILogger log)
    {
        telemetryClient.TrackEvent($"{FunctionName} Azure func start");
        log.LogInformation($"{FunctionName} Azure func starting at {DateTime.Now} on {Environment.MachineName}");

        var mailSender = GetSenderEmail();
        var admins = await GetAdminsAsync();
        var allUsers = await GetAllUsersAsync();
        var mailBody = new StatsToAdminMailBuilder(FunctionName, startTime/*, admins, allUsers*/).GetMailBody();

        var msg = new SendGridMessage()
        {
            From = mailSender,
            Subject = $"MemCheck stats",
            HtmlContent = mailBody
        };

        admins.ForEach(e => msg.AddTo(e.Email, e.Name));
        msg.AddBcc(mailSender);
        msg.SetClickTracking(false, false);

        var response = await GetSendGridClient().SendEmailAsync(msg);

        log.LogInformation($"Mail sent, status code {response.StatusCode}");
        log.LogInformation($"Response body: {await response.Body.ReadAsStringAsync()}");

        log.LogInformation($"Function '{FunctionName}' ending, {DateTime.Now}");
    }
}
