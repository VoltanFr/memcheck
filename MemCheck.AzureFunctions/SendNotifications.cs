using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MemCheck.Application.Notifiying;
using MemCheck.Application.Users;
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
    public SendNotifications(TelemetryConfiguration telemetryConfiguration, MemCheckDbContext memCheckDbContext, MemCheckUserManager userManager, ILogger<SendNotifications> logger)
        : base(telemetryConfiguration, memCheckDbContext, userManager, logger)
    {
    }
    [FunctionName(nameof(SendNotifications))]
    public async Task Run([TimerTrigger(Constants.Cron_SendNotifications)] TimerInfo timer, ExecutionContext context)
    {
        await RunAsync(timer, context).ConfigureAwait(false);
    }
    protected override async Task<string> RunAndCreateReportMailMainPartAsync()
    {
        var mailer = new NotificationMailer(NewCallContext(), new MemCheckMailSender(MailSender), new MemCheckLinkGenerator());
        return await mailer.RunAndCreateReportMailMainPartAsync().ConfigureAwait(false);
    }
}
