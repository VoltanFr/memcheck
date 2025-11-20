using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MemCheck.Application;
using MemCheck.Application.QueryValidation;
using MemCheck.Application.Users;
using MemCheck.AzureComponents;
using MemCheck.Basics;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace MemCheck.AzureFunctions;

public abstract class AbstractMemCheckAzureFunction
{
    #region Fields
    private readonly TelemetryClient telemetryClient;
    private readonly MemCheckDbContext memCheckDbContext;
    private readonly ILogger logger;
    private readonly IRoleChecker roleChecker;
    private readonly Lazy<Task<ImmutableList<MemCheckEmailAddress>>> admins;
    #endregion
    #region Private methods
    private async Task<ImmutableList<MemCheckEmailAddress>> GetAdminsAsync()
    {
        var getter = new GetAdminEmailAddesses(NewCallContext());
        var getterResult = await getter.RunAsync(new GetAdminEmailAddesses.Request(AdminUserId));
        var result = getterResult.Users;
        return result.Select(address => new MemCheckEmailAddress(address.Email, address.Name)).ToImmutableList();
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
            MailSender = GetMailSender();
            admins = new Lazy<Task<ImmutableList<MemCheckEmailAddress>>>(GetAdminsAsync);
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
    private static AzureMailSender GetMailSender()
    {
        var connectionString = Environment.GetEnvironmentVariable("AzureMailConnectionString");
        if (connectionString == null)
            throw new InvalidOperationException("'AzureMailConnectionString' environment variable is not set");

        var recipientToAddInBccOfAllMails = Environment.GetEnvironmentVariable("RecipientToAddInBccOfAllMails");
        if (recipientToAddInBccOfAllMails == null)
            throw new InvalidOperationException("'RecipientToAddInBccOfAllMails' environment variable is not set");
        return new AzureMailSender(connectionString, recipientToAddInBccOfAllMails);
    }
    protected CallContext NewCallContext()
    {
        return new CallContext(memCheckDbContext, new MemCheckTelemetryClient(telemetryClient), new FakeStringLocalizer(), roleChecker);
    }
    private string GetMailFooter(string azureFunctionName, TimerInfo timer, DateTime azureFunctionStartTime, ImmutableList<MemCheckEmailAddress> admins)
    {
        var listItems = new List<string> {
            $"<li>Sent by Azure func {azureFunctionName} ({AssemblyServices.GetDisplayInfoForAssembly(GetType().Assembly)})</li>",
            $"<li>Running on {Environment.MachineName}, process id: {Environment.ProcessId}, process name: {Process.GetCurrentProcess().ProcessName}, started on: {DateServices.AsIsoWithHHmm(Process.GetCurrentProcess().StartTime)}, peak mem usage: {ProcessServices.GetPeakProcessMemoryUsage()} bytes</li>",
            $"<li>Started on {DateServices.AsIsoWithHHmmss(azureFunctionStartTime)}, mail constructed at {DateServices.AsIsoWithHHmmss(DateTime.UtcNow)} (Elapsed: {(DateTime.UtcNow - azureFunctionStartTime).ToStringWithoutMs()})</li>",
            $"<li>Sent to {admins.Count} admins: {string.Join(",", admins.Select(a => a.DisplayName))}</li>"
        };
        if (timer.ScheduleStatus != null)
            listItems.AddRange(new[]
                {
                    $"<li>Function last schedule: {DateServices.AsIsoWithHHmmss(timer.ScheduleStatus.Last)}</li>",
                    $"<li>Function last schedule updated: {DateServices.AsIsoWithHHmmss(timer.ScheduleStatus.LastUpdated)}</li>",
                    $"<li>Function next schedule: {DateServices.AsIsoWithHHmmss(timer.ScheduleStatus.Next)}</li>",
                });

        var writer = new StringBuilder()
            .Append("<h2>Info</h2>")
            .Append(CultureInfo.InvariantCulture, $"<ul>{string.Join("", listItems)}</ul>");

        return writer.ToString();
    }
    private static string GetAssemblyVersion()
    {
        return typeof(AbstractMemCheckAzureFunction).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
    }
    private static void AddExceptionDetailsToMailBody(StringBuilder body, Exception e)
    {
        body = body
            .Append(CultureInfo.InvariantCulture, $"<p>Caught {e.GetType().Name}</p>")
            .Append(CultureInfo.InvariantCulture, $"<p>Message: {e.Message}</p>");

        if (e.StackTrace != null)
            body = body.Append(CultureInfo.InvariantCulture, $"<p>Call stack: {e.StackTrace.Replace("\n", "<br/>", StringComparison.Ordinal)}</p>");

        if (e.InnerException != null)
        {
            body = body.Append(CultureInfo.InvariantCulture, $"<p>-------- Inner ---------</p>");
            AddExceptionDetailsToMailBody(body, e.InnerException);
        }
    }
    public async Task SendFailureInfoMailAsync(string functionName, Exception e)
    {
        var body = new StringBuilder()
            .Append(CultureInfo.InvariantCulture, $"<h1>Mnesios function '{functionName}' failure</h1>")
            .Append(CultureInfo.InvariantCulture, $"<p>Sent by Azure func '{functionName}' {GetAssemblyVersion()} running on {Environment.MachineName}, started on {StartTime}, mail constructed at {DateTime.UtcNow}</p>");

        AddExceptionDetailsToMailBody(body, e);

        await MailSender.SendAsync(await AdminsAsync().ConfigureAwait(false), "Mnesios Azure function failure", body.ToString());
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
                .Append(GetMailFooter(context.FunctionDefinition.Name, timer, StartTime, await AdminsAsync()));

            var bodyText = reportMailBody.ToString();

            await MailSender.SendAsync(await AdminsAsync(), runResult.MailSubject, bodyText);
        }
        catch (Exception ex)
        {
            await SendFailureInfoMailAsync(context.FunctionDefinition.Name, ex);
        }
        finally
        {
            logger.LogInformation("Function '{FunctionName}' ending, {Now}", context.FunctionDefinition.Name, DateTime.Now);
        }
    }
    protected abstract Task<RunResult> RunAndCreateReportMailMainPartAsync(string defaultMailSubject);
    protected DateTime StartTime { get; }
    protected IMemCheckMailSender MailSender { get; }
    protected Guid AdminUserId { get; }
    protected Guid BotUserId { get; }
    protected async Task<ImmutableList<MemCheckEmailAddress>> AdminsAsync() => await admins.Value;
}
