using System.Collections.Generic;
using System.Threading.Tasks;
using MemCheck.Application.Notifying;
using MemCheck.Application.Users;
using MemCheck.Basics;
using MemCheck.Database;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SendGrid.Helpers.Mail;

namespace MemCheck.AzureFunctions;

internal sealed class SendNotifications : AbstractMemCheckAzureFunction
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
            await mailSender.SendAsync(subject, body, new EmailAddress(to).AsArray());
        }
    }
    private sealed class MemCheckLinkGenerator : IMemCheckLinkGenerator
    {
        public string GetAbsoluteUri(string relativeUri)
        {
            return "https://memcheckfr.azurewebsites.net" + (relativeUri.StartsWith("/") ? "" : "/") + relativeUri;
        }
    }
    #endregion
    #region Fields
    private const string FuncName = nameof(SendNotifications);
    #endregion
    public SendNotifications(TelemetryConfiguration telemetryConfiguration, MemCheckDbContext memCheckDbContext, MemCheckUserManager userManager, ILogger<SendStatsToAdministrators> logger)
        : base(telemetryConfiguration, memCheckDbContext, userManager, logger)
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
        await RunAsync();
    }
    protected override string FunctionName => FuncName;
    protected async override Task DoRunAsync()
    {
        var mailer = new NotificationMailer(NewCallContext(), MailSender.SenderEmail.Email, new MemCheckMailSender(MailSender), new MemCheckLinkGenerator());
        await mailer.RunAsync();
    }
}
