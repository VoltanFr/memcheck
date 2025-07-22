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
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SendGrid.Helpers.Mail;

namespace MemCheck.AzureFunctions;

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
    private Guid GetUserIdFromEnv(string envVarName)
    {
        var envVarValue = Environment.GetEnvironmentVariable(envVarName);
        if (envVarValue == null)
        {
            logger.LogError("{EnvVarName} is null", envVarName);
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
            AdminUserId = GetUserIdFromEnv("AdminUserId");
            BotUserId = GetUserIdFromEnv("BotAccountUserId");
            roleChecker = new ProdRoleChecker(userManager);
            StartTime = DateTime.UtcNow;
            MailSender = new AzureFunctionsMailSender(StartTime, logger);
            admins = new Lazy<Task<ImmutableList<EmailAddress>>>(GetAdminsAsync);
        }
        catch (Exception e)
        {
            logger.LogError("Exception in AbstractMemCheckAzureFunction.Constructor for {FunctionType}", GetType().Name);
            logger.LogError("Caught {ExceptionType}", e.GetType().Name);
            logger.LogError("Message: '{Message}'", e.Message);
            logger.LogError("Stack");
            if (e.StackTrace != null)
                logger.LogError("{StackTrace}", e.StackTrace.Replace("\n", "<br/>\t", StringComparison.Ordinal));
            if (e.InnerException != null)
            {
                logger.LogError("****** Inner");
                logger.LogError("Caught {InnerType}", e.InnerException.GetType().Name);
                logger.LogError("Message: '{InnerMessage}'", e.InnerException.Message);
                logger.LogError("Stack");
                if (e.InnerException.StackTrace != null)
                    logger.LogError("{InnerStackTrace}", e.InnerException.StackTrace.Replace("\n", "<br/>\t", StringComparison.Ordinal));
            }
            else
                logger.LogError("No inner");
            logger.LogError("****** Env vars");
            var envVars = Environment.GetEnvironmentVariables();
            foreach (DictionaryEntry envVar in envVars)
                logger.LogError("'{EnvVarKey}'='{EnvVarValue}'", envVar.Key, envVar.Value);
            throw;
        }
    }
    protected CallContext NewCallContext()
    {
        return new CallContext(memCheckDbContext, new MemCheckTelemetryClient(telemetryClient), new FakeStringLocalizer(), roleChecker);
    }
    protected async Task RunAsync(TimerInfo timer, FunctionContext context)
    {
        try
        {
            logger.LogInformation("{FunctionName} Azure func starting at {StartTime} on {MachineName}", context.FunctionDefinition.Name, DateTime.Now, Environment.MachineName);
            telemetryClient.TrackEvent($"{context.FunctionDefinition.Name} Azure func start");
            var runResult = await RunAndCreateReportMailMainPartAsync(context.FunctionDefinition.Name);

            var reportMailBody = new StringBuilder()
                .Append("<style>")
                .Append("thead{background-color:darkgray;color:white;}")
                .Append("table{border-width:1px;border-color:green;border-collapse:collapse;}")
                .Append("tr{border-width:1px;border-style:solid}")
                .Append("td{border-width:1px;border-style:solid}")
                .Append("tr:nth-child(odd) {background-color: lavender;}")
                .Append("</style>")
                .Append(CultureInfo.InvariantCulture, $"<h1>{context.FunctionDefinition.Name}</h1>")
                .Append(runResult.MailBodyMainPart)
                .Append(MailSender.GetMailFooter(context.FunctionDefinition.Name, timer, StartTime, await AdminsAsync()));

            var bodyText = reportMailBody.ToString();

            await MailSender.SendAsync(runResult.MailSubject, bodyText, await AdminsAsync());
        }
        catch (Exception ex)
        {
            await MailSender.SendFailureInfoMailAsync(context.FunctionDefinition.Name, ex);
        }
        finally
        {
            logger.LogInformation("Function '{FunctionName}' ending, {Now}", context.FunctionDefinition.Name, DateTime.Now);
        }
    }
    protected abstract Task<RunResult> RunAndCreateReportMailMainPartAsync(string defaultMailSubject);
    protected DateTime StartTime { get; }
    protected AzureFunctionsMailSender MailSender { get; }
    protected Guid AdminUserId { get; }
    protected Guid BotUserId { get; }
    protected async Task<ImmutableList<EmailAddress>> AdminsAsync() => await admins.Value;
}
