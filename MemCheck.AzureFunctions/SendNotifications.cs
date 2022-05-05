using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MemCheck.Application.Notifiying;
using MemCheck.Basics;
using MemCheck.Database;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SendGrid.Helpers.Mail;

namespace MemCheck.AzureFunctions;

public sealed class SendNotifications : AbstractMemCheckAzureFunction
{
    #region Private classes MemCheckMailSender & MemCheckLinkGenerator
    private sealed class MemCheckMailSender : IMemCheckMailSender
    {
        #region Fields
        private readonly MailSender mailSender;
        #endregion
        public MemCheckMailSender(MailSender mailSender)
        {
            this.mailSender = mailSender;
        }
        public async Task SendAsync(string to, string subject, string body)
        {
            await mailSender.SendAsync(subject, body, new EmailAddress(to).AsArray()).ConfigureAwait(false);
        }
    }
    private sealed class MemCheckLinkGenerator : IMemCheckLinkGenerator
    {
        public string GetAbsoluteAddress(string relativeUri)
        {
            return "https://memcheckfr.azurewebsites.net" + (relativeUri.StartsWith("/", StringComparison.InvariantCulture) ? "" : "/") + relativeUri;
        }
    }
    #endregion
    #region Fields
    private const string FuncName = nameof(SendNotifications);
    #endregion
    public SendNotifications(TelemetryConfiguration telemetryConfiguration, MemCheckDbContext memCheckDbContext, ILogger<SendNotifications> logger)
        : base(telemetryConfiguration, memCheckDbContext, logger)
    {
    }
    [FunctionName(FuncName)]
    public async Task Run([TimerTrigger(
        Constants.CronAt4Daily
        #if DEBUG
        //, RunOnStartup = true
        #endif
        )] TimerInfo myTimer, ExecutionContext context)
    {
        await RunAsync().ConfigureAwait(false);
    }
    protected override string FunctionName => FuncName;
    protected override async Task DoRunAsync()
    {
        var mailer = new NotificationMailer(NewCallContext(), MailSender.SenderEmail.Email, new MemCheckMailSender(MailSender), new MemCheckLinkGenerator());
        await mailer.RunAsync().ConfigureAwait(false);
    }
}
