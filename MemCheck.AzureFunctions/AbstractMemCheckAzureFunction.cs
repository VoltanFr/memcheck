using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using MemCheck.Application;
using MemCheck.Application.QueryValidation;
using MemCheck.Application.Users;
using MemCheck.Database;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;
using SendGrid.Helpers.Mail;

namespace MemCheck.AzureFunctions;

internal abstract class AbstractMemCheckAzureFunction
{
    #region Fields
    private readonly TelemetryClient telemetryClient;
    private readonly MemCheckDbContext memCheckDbContext;
    private readonly MemCheckUserManager userManager;
    private readonly ILogger logger;
    private readonly IRoleChecker roleChecker;
    private readonly Lazy<Task<ImmutableList<EmailAddress>>> admins;
    #endregion
    #region Private methods
    private async Task<ImmutableList<EmailAddress>> GetAdminsAsync()
    {
        var getter = new GetAdminEmailAddesses(NewCallContext());
        var getterResult = await getter.RunAsync(new GetAdminEmailAddesses.Request(RunningUserId));
        var result = getterResult.Users;
        return result.Select(address => new EmailAddress(address.Email, address.Name)).ToImmutableList();
    }
    #endregion
    protected AbstractMemCheckAzureFunction(TelemetryConfiguration telemetryConfiguration, MemCheckDbContext memCheckDbContext, MemCheckUserManager userManager, ILogger logger)
    {
        telemetryClient = new TelemetryClient(telemetryConfiguration);
        this.memCheckDbContext = memCheckDbContext;
        this.userManager = userManager;
        this.logger = logger;
        roleChecker = new ProdRoleChecker(userManager); ;
        RunningUserId = new Guid(Environment.GetEnvironmentVariable("RunningUserId"));
        StartTime = DateTime.UtcNow;
        MailSender = new MailSender(FunctionName, StartTime, logger);
        admins = new Lazy<Task<ImmutableList<EmailAddress>>>(GetAdminsAsync);
    }
    protected CallContext NewCallContext()
    {
        return new CallContext(memCheckDbContext, new MemCheckTelemetryClient(telemetryClient), new FakeStringLocalizer(), roleChecker);
    }
    protected MemCheckUserManager UserManager { get => userManager; }
    protected abstract string FunctionName { get; }
    protected async Task RunAsync()
    {
        try
        {
            logger.LogInformation($"{FunctionName} Azure func starting at {DateTime.Now} on {Environment.MachineName}");
            telemetryClient.TrackEvent($"{FunctionName} Azure func start");
            await DoRunAsync();
        }
        catch (Exception ex)
        {
            await MailSender.SendFailureInfoMailAsync(ex);
        }
        finally
        {
            logger.LogInformation($"Function '{FunctionName}' ending, {DateTime.Now}");
        }
    }
    protected abstract Task DoRunAsync();
    protected DateTime StartTime { get; }
    protected MailSender MailSender { get; }
    protected Guid RunningUserId { get; }
    protected async Task<ImmutableList<EmailAddress>> AdminsAsync() => await admins.Value;
}
