using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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

public sealed class SendStatsToAdministrators
{
    #region Fields
    private const string FunctionName = nameof(SendStatsToAdministrators);
    private readonly TelemetryClient telemetryClient;
    private readonly MemCheckDbContext memCheckDbContext;
    private readonly MemCheckUserManager userManager;
    private readonly IRoleChecker roleChecker;
    private readonly Guid runningUserId;
    private readonly DateTime startTime;
    private readonly EmailAddress senderEmail;
    #endregion
    #region Private methods
    private SendGridClient GetSendGridClient()
    {
        var sendGridKey = Environment.GetEnvironmentVariable("SendGridKey");
        return new SendGridClient(sendGridKey);
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
    private async Task SendFailureInfoMailAsync(Exception e)
    {
        var writer = new StringBuilder();

        writer.Append($"<h1>MemCheck function '{FunctionName}' failure</h1>");
        var version = GetType().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
        writer.Append($"<p>Sent by Azure func '{FunctionName}' {version} running on {Environment.MachineName}, started on {startTime}, mail constructed at {DateTime.UtcNow}</p>");
        writer.Append($"<p>Caught {e.GetType().Name}</p>");
        writer.Append($"<p>Message: {e.Message}</p>");
        writer.Append($"<p>Call stack: {e.StackTrace}</p>");

        var msg = new SendGridMessage()
        {
            From = senderEmail,
            Subject = $"MemCheck Azure function failure",
            HtmlContent = writer.ToString()
        };

        msg.AddTo(senderEmail);
        msg.SetClickTracking(false, false);

        await GetSendGridClient().SendEmailAsync(msg);
    }
    #endregion
    public SendStatsToAdministrators(TelemetryConfiguration telemetryConfiguration, MemCheckDbContext memCheckDbContext, MemCheckUserManager userManager)
    {
        telemetryClient = new TelemetryClient(telemetryConfiguration);
        this.memCheckDbContext = memCheckDbContext;
        this.userManager = userManager;
        roleChecker = new ProdRoleChecker(userManager); ;
        runningUserId = new Guid(Environment.GetEnvironmentVariable("RunningUserId"));
        var sendGridSender = Environment.GetEnvironmentVariable("SendGridSender");
        var sendGridUser = Environment.GetEnvironmentVariable("SendGridUser");
        senderEmail = new EmailAddress(sendGridSender, sendGridUser);
        startTime = DateTime.UtcNow;
    }
    [FunctionName(FunctionName)]
    public async Task Run([TimerTrigger(
        //"0 */8 * * *"
        "*/5 * * * *"
        #if DEBUG
        , RunOnStartup = true
        #endif
        )] TimerInfo myTimer, ExecutionContext context, ILogger log)
    {
        try
        {
            telemetryClient.TrackEvent($"{FunctionName} Azure func start");
            log.LogInformation($"{FunctionName} Azure func starting at {DateTime.Now} on {Environment.MachineName}");

            var admins = await GetAdminsAsync();
            var allUsers = await GetAllUsersAsync();
            var mailBody = new StatsToAdminMailBuilder(FunctionName, startTime, admins, allUsers).GetMailBody();

            var msg = new SendGridMessage()
            {
                From = senderEmail,
                Subject = $"MemCheck stats",
                HtmlContent = mailBody
            };

            admins.ForEach(e => msg.AddTo(e.Email, e.Name));
            msg.AddBcc(senderEmail);
            msg.SetClickTracking(false, false);

            var response = await GetSendGridClient().SendEmailAsync(msg);

            log.LogInformation($"Mail sent, status code {response.StatusCode}");
            log.LogInformation($"Response body: {await response.Body.ReadAsStringAsync()}");

            log.LogInformation($"Function '{FunctionName}' ending, {DateTime.Now}");
        }
        catch (Exception ex)
        {
            await SendFailureInfoMailAsync(ex);
        }
    }
}
