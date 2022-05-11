﻿using System;
using System.Threading.Tasks;
using MemCheck.Application.Users;
using MemCheck.Database;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace MemCheck.AzureFunctions;

public sealed class SendHelloToAdministrators : AbstractMemCheckAzureFunction
{
    #region Fields
    private const string FuncName = nameof(SendHelloToAdministrators);
    #endregion
    public SendHelloToAdministrators(TelemetryConfiguration telemetryConfiguration, MemCheckDbContext memCheckDbContext, MemCheckUserManager userManager, ILogger<SendHelloToAdministrators> logger)
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
        var mailBody = $"<h1>MemCheck says hello</h1><h2>Time here</h2><p>{DateTime.Now}</p>" + MailSender.GetMailFooter(FunctionName, StartTime, await AdminsAsync());
        await MailSender.SendAsync("MemCheck hello", mailBody, await AdminsAsync());
    }
}
