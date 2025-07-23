using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MemCheck.Domain;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace MemCheck.AzureFunctions;

public sealed class AzureFunctionsMailSender
{
    #region Fields
    private readonly ILogger logger;
    private readonly string sendGridKey;
    #endregion
    #region Private methods
    #endregion
    public AzureFunctionsMailSender(ILogger logger)
    {
        this.logger = logger;
        var sendGridSender = Environment.GetEnvironmentVariable("SendGridSender");
        var sendGridUser = Environment.GetEnvironmentVariable("SendGridUser");
        SenderEmail = new MemCheckEmailAddress(sendGridSender ?? "", sendGridUser ?? "");
        sendGridKey = Environment.GetEnvironmentVariable("SendGridKey") ?? "NoSendGridKey";
    }
    public async Task SendAsync(string subject, string body, IEnumerable<EmailAddress> to)
    {
        var msg = new SendGridMessage()
        {
            From = new EmailAddress(SenderEmail.Address, SenderEmail.DisplayName),
            Subject = subject,
            HtmlContent = body
        };

        to.ToList().ForEach(address => msg.AddTo(address.Email, address.Name));
        msg.AddTo(new EmailAddress(SenderEmail.Address, SenderEmail.DisplayName));
        msg.SetClickTracking(false, false);

        var sendGridClient = new SendGridClient(sendGridKey);
        var response = await sendGridClient.SendEmailAsync(msg);

        logger.LogInformation("Mail sent, status code {StatusCode}", response.StatusCode);
        logger.LogInformation("Response body: {ResponseBody}", await response.Body.ReadAsStringAsync());
    }
    public MemCheckEmailAddress SenderEmail { get; }
}
