using System;
using System.Collections;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MemCheck.Application;
using MemCheck.Application.QueryValidation;
using MemCheck.Application.Users;
using MemCheck.Database;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SendGrid.Helpers.Mail;

namespace MemCheck.AzureFunctions;

public sealed record RunResult(string MailSubject, StringBuilder MailBodyMainPart);

public abstract class AbstractMemCheckAzureFunction
{
    #region Fields
    private readonly TelemetryClient telemetryClient;
    private readonly MemCheckDbContext memCheckDbContext;
    private readonly ILogger logger;
    private readonly IRoleChecker roleChecker;
    private readonly Lazy<Task<ImmutableList<EmailAddress>>> admins;
    #endregion
    #region Private methods
    private async Task<ImmutableList<EmailAddress>> GetAdminsAsync()
    {
        var getter = new GetAdminEmailAddesses(NewCallContext());
        var getterResult = await getter.RunAsync(new GetAdminEmailAddesses.Request(AdminUserId));
        var result = getterResult.Users;
        return result.Select(address => new EmailAddress(address.Email, address.Name)).ToImmutableList();
    }
    private static Guid GetUserIdFromEnv(string envVarName, ILogger logger)
    {
        var envVarValue = Environment.GetEnvironmentVariable(envVarName);
        if (envVarValue == null)
        {
            logger.LogError($"{envVarName} is null");
            return Guid.Empty;
        }
        return new Guid(envVarValue);
    }
    #endregion
    protected AbstractMemCheckAzureFunction(TelemetryConfiguration telemetryConfiguration, MemCheckDbContext memCheckDbContext, MemCheckUserManager userManager, ILogger logger)
    {
        try
        {
            telemetryClient = new TelemetryClient(telemetryConfiguration);
            this.memCheckDbContext = memCheckDbContext;
            this.logger = logger;
            AdminUserId = GetUserIdFromEnv("AdminUserId", logger);
            BotUserId = GetUserIdFromEnv("BotAccountUserId", logger);
            roleChecker = new ProdRoleChecker(userManager);
            StartTime = DateTime.UtcNow;
            MailSender = new MailSender(StartTime, logger);
            admins = new Lazy<Task<ImmutableList<EmailAddress>>>(GetAdminsAsync);
        }
        catch (Exception e)
        {
            logger.LogError($"Exception in AbstractMemCheckAzureFunction.Constructor for {GetType().Name}");
            logger.LogError($"Caught {e.GetType().Name}");
            logger.LogError($"Message: '{e.Message}'");
            logger.LogError("Stack");
            if (e.StackTrace != null)
                logger.LogError(e.StackTrace.Replace("\n", "<br/>\t", StringComparison.Ordinal));
            if (e.InnerException != null)
            {
                logger.LogError("****** Inner");
                logger.LogError($"Caught {e.InnerException.GetType().Name}");
                logger.LogError($"Message: '{e.InnerException.Message}'");
                logger.LogError("Stack");
                if (e.InnerException.StackTrace != null)
                    logger.LogError(e.InnerException.StackTrace.Replace("\n", "<br/>\t", StringComparison.Ordinal));
            }
            else
                logger.LogError("No inner");
            logger.LogError("****** Env vars");
            var envVars = Environment.GetEnvironmentVariables();
            foreach (DictionaryEntry envVar in envVars)
                logger.LogError($"'{envVar.Key}'='{envVar.Value}'");
            throw;
        }
    }
    protected CallContext NewCallContext()
    {
        return new CallContext(memCheckDbContext, new MemCheckTelemetryClient(telemetryClient), new FakeStringLocalizer(), roleChecker);
    }
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Really want to catch all possible problems")]
    protected async Task RunAsync(TimerInfo timer, ExecutionContext context)
    {
        try
        {
            logger.LogInformation($"{context.FunctionName} Azure func starting at {DateTime.Now} on {Environment.MachineName}");
            telemetryClient.TrackEvent($"{context.FunctionName} Azure func start");
            var runResult = await RunAndCreateReportMailMainPartAsync(context.FunctionName);

            var reportMailBody = new StringBuilder()
                .Append("<style>")
                .Append("thead{background-color:darkgray;color:white;}")
                .Append("table{border-width:1px;border-color:green;border-collapse:collapse;}")
                .Append("tr{border-width:1px;border-style:solid}")
                .Append("td{border-width:1px;border-style:solid}")
                .Append("tr:nth-child(odd) {background-color: lavender;}")
                .Append("</style>")
                .Append(CultureInfo.InvariantCulture, $"<h1>{context.FunctionName}</h1>")
                .Append(runResult.MailBodyMainPart)
                .Append(MailSender.GetMailFooter(context.FunctionName, timer, StartTime, await AdminsAsync()));

            var bodyText = reportMailBody.ToString();

            await MailSender.SendAsync(runResult.MailSubject, bodyText, await AdminsAsync());
        }
        catch (Exception ex)
        {
            await MailSender.SendFailureInfoMailAsync(context.FunctionName, ex);
        }
        finally
        {
            logger.LogInformation($"Function '{context.FunctionName}' ending, {DateTime.Now}");
        }
    }
    protected abstract Task<RunResult> RunAndCreateReportMailMainPartAsync(string defaultMailSubject);
    protected DateTime StartTime { get; }
    protected MailSender MailSender { get; }
    protected Guid AdminUserId { get; }
    protected Guid BotUserId { get; }
    protected async Task<ImmutableList<EmailAddress>> AdminsAsync() => await admins.Value;
}
