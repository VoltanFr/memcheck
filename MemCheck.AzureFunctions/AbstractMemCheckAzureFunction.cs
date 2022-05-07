﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using MemCheck.Application;
using MemCheck.Application.QueryValidation;
using MemCheck.Application.Users;
using MemCheck.Basics;
using MemCheck.Database;
using MemCheck.Domain;
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


public sealed class AzureFuncRoleChecker : IRoleChecker
{
    #region Fields
    private readonly ImmutableHashSet<Guid> admins;
    #endregion
    #region Private method
    #endregion
    public AzureFuncRoleChecker(params Guid[] admins)
    {
        this.admins = admins.ToImmutableHashSet();
    }
    public async Task<bool> UserIsAdminAsync(MemCheckDbContext dbContext, Guid userId)
    {
        await Task.CompletedTask;
        return admins.Contains(userId);
    }
    public async Task<bool> UserIsAdminAsync(MemCheckUser user)
    {
        await Task.CompletedTask;
        return admins.Contains(user.Id);
    }
    public async Task<IEnumerable<string>> GetRolesAsync(MemCheckUser user)
    {
        return await UserIsAdminAsync(user) ? IRoleChecker.AdminRoleName.AsArray() : Array.Empty<string>();
    }
}


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
    protected AbstractMemCheckAzureFunction(TelemetryConfiguration telemetryConfiguration, MemCheckDbContext memCheckDbContext, ILogger logger)
    {
        try
        {
            telemetryClient = new TelemetryClient(telemetryConfiguration);
            this.memCheckDbContext = memCheckDbContext;
            this.logger = logger;
            RunningUserId = new Guid(Environment.GetEnvironmentVariable("RunningUserId"));
            roleChecker = new AzureFuncRoleChecker(RunningUserId);
            StartTime = DateTime.UtcNow;
            MailSender = new MailSender(FunctionName, StartTime, logger);
            admins = new Lazy<Task<ImmutableList<EmailAddress>>>(GetAdminsAsync);
        }
        catch (Exception e)
        {
            Console.WriteLine($"In AbstractMemCheckAzureFunction.Constructor for {GetType().Name}");
            Console.WriteLine($"Caught {e.GetType().Name}");
            Console.WriteLine($"Message: '{e.Message}'");
            Console.WriteLine("Stack");
            Console.WriteLine(e.StackTrace.Replace("\n", "<br/>\t", StringComparison.Ordinal));
            if (e.InnerException != null)
            {
                Console.WriteLine("****** Inner");
                Console.WriteLine($"Caught {e.InnerException.GetType().Name}");
                Console.WriteLine($"Message: '{e.InnerException.Message}'");
                Console.WriteLine("Stack");
                Console.WriteLine(e.InnerException.StackTrace.Replace("\n", "<br/>\t", StringComparison.Ordinal));
            }
            else
                Console.WriteLine("No inner");
            Console.WriteLine("****** Env vars");
            var envVars = Environment.GetEnvironmentVariables();
            foreach (DictionaryEntry envVar in envVars)
                Console.WriteLine($"'{envVar.Key}'='{envVar.Value}'");
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
