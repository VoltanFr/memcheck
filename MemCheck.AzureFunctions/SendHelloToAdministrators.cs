using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MemCheck.Basics;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SendGrid.Helpers.Mail;

namespace MemCheck.AzureFunctions;

public sealed class SendHelloToAdministrators
{
    #region Fields
    private readonly ILogger logger;
    private const string FuncName = nameof(SendHelloToAdministrators);
    #endregion
    public SendHelloToAdministrators(ILogger<SendHelloToAdministrators> logger)
    {
        this.logger = logger;
    }
    [FunctionName(FuncName)]
    public async Task Run([TimerTrigger(
        Constants.CronEvery5Min
        #if DEBUG
        , RunOnStartup = true
        #endif
        )] TimerInfo myTimer, ExecutionContext context)
    {
        var mailSender = new MailSender(FuncName, DateTime.UtcNow, logger);
        var mailBody = $"<h1>MemCheck says hello</h1><h2>Time here</h2><p>{DateTime.Now}</p>";
        await mailSender.SendAsync("MemCheck hello", mailBody, new EmailAddress("MemCheckAdm@gmail.com").AsArray());
    }
}
