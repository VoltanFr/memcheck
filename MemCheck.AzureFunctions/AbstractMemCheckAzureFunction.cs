﻿using System;
using System.Collections;
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

//This is not nice. I'm turning around the security and pretending the user id obtained as a parameter is sure to be an admin
//It would be much better to request this info from a UserManager, using the real security.
//Unfortunately, I failed to use a UserManager, with very obscure problems: when I add the commented code below in Startup.Configure, I get some errors in Azure with this message: System.InvalidOperationException : No authentication handlers are registered. Did you forget to call AddAuthentication().Add[SomeAuthHandler]("ArmToken",...)?
//builder.Services.AddIdentityCore<MemCheckUser>(opt => { opt.SignIn.RequireConfirmedAccount = true; opt.User.RequireUniqueEmail = false; })
//    .AddRoles<MemCheckUserRole>()
//    .AddUserManager<MemCheckUserManager>()
//    .AddEntityFrameworkStores<MemCheckDbContext>();


//public sealed class AzureFuncRoleChecker : IRoleChecker
//{
//    #region Fields
//    private readonly ImmutableHashSet<Guid> admins;
//    #endregion
//    #region Private method
//    #endregion
//    public AzureFuncRoleChecker(params Guid[] admins)
//    {
//        this.admins = admins.ToImmutableHashSet();
//    }
//    public async Task<bool> UserIsAdminAsync(MemCheckDbContext dbContext, Guid userId)
//    {
//        await Task.CompletedTask;
//        return admins.Contains(userId);
//    }
//    public async Task<bool> UserIsAdminAsync(MemCheckUser user)
//    {
//        await Task.CompletedTask;
//        return admins.Contains(user.Id);
//    }
//    public async Task<IEnumerable<string>> GetRolesAsync(MemCheckUser user)
//    {
//        return await UserIsAdminAsync(user) ? IRoleChecker.AdminRoleName.AsArray() : Array.Empty<string>();
//    }
//}

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
        var getterResult = await getter.RunAsync(new GetAdminEmailAddesses.Request(RunningUserId));
        var result = getterResult.Users;
        return result.Select(address => new EmailAddress(address.Email, address.Name)).ToImmutableList();
    }
    #endregion
    protected AbstractMemCheckAzureFunction(TelemetryConfiguration telemetryConfiguration, MemCheckDbContext memCheckDbContext, MemCheckUserManager userManager, ILogger logger)
    {
        try
        {
            logger.LogInformation($"In AbstractMemCheckAzureFunction.Constructor for {GetType().Name}");
            var envVars = Environment.GetEnvironmentVariables();
            logger.LogInformation("Logging env vars");
            foreach (DictionaryEntry envVar in envVars)
                logger.LogInformation($"'{envVar.Key}'='{envVar.Value}'");
            logger.LogInformation("Creating telemetryClient");
            telemetryClient = new TelemetryClient(telemetryConfiguration);
            logger.LogInformation("Assigning memCheckDbContext");
            this.memCheckDbContext = memCheckDbContext;
            logger.LogInformation("Assigning logger");
            this.logger = logger;
            logger.LogInformation("Assigning runningUserIdEnvVar");
            string runningUserIdEnvVar = Environment.GetEnvironmentVariable("RunningUserId");
            if (runningUserIdEnvVar == null)
            {
                logger.LogError("runningUserIdEnvVar is null");
                RunningUserId = Guid.Empty;
            }
            else
            {
                logger.LogInformation($"runningUserIdEnvVar = '{runningUserIdEnvVar}'");
                RunningUserId = new Guid(runningUserIdEnvVar);
            }
            logger.LogInformation("Assigning roleChecker");
            roleChecker = new ProdRoleChecker(userManager);
            logger.LogInformation("Assigning StartTime");
            StartTime = DateTime.UtcNow;
            logger.LogInformation("Assigning MailSender");
            MailSender = new MailSender(FunctionName, StartTime, logger);
            logger.LogInformation("Assigning admins");
            admins = new Lazy<Task<ImmutableList<EmailAddress>>>(GetAdminsAsync);
            logger.LogInformation("End of constructor");
        }
        catch (Exception e)
        {
            logger.LogError($"Exception in AbstractMemCheckAzureFunction.Constructor for {GetType().Name}");
            logger.LogError($"Caught {e.GetType().Name}");
            logger.LogError($"Message: '{e.Message}'");
            logger.LogError("Stack");
            logger.LogError(e.StackTrace.Replace("\n", "<br/>\t", StringComparison.Ordinal));
            if (e.InnerException != null)
            {
                logger.LogError("****** Inner");
                logger.LogError($"Caught {e.InnerException.GetType().Name}");
                logger.LogError($"Message: '{e.InnerException.Message}'");
                logger.LogError("Stack");
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
    protected abstract string FunctionName { get; }
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Really want to catch all possible problems")]
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
