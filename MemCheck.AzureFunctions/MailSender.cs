using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MemCheck.Basics;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace MemCheck.AzureFunctions;

internal sealed class MailSender
{
    #region Fields
    private readonly string functionName;
    private readonly ILogger logger;
    private readonly DateTime functionStartTime;
    private readonly string sendGridKey;
    #endregion
    #region Private methods
    private void AddExceptionDetailsToMailBody(StringBuilder body, Exception e)
    {
        body.Append($"<p>Caught {e.GetType().Name}</p>");
        body.Append($"<p>Message: {e.Message}</p>");
        body.Append($"<p>Call stack: {e.StackTrace.Replace("\n","<br/>")}</p>");

        if (e.InnerException != null)
        {
            body.Append($"<p>-------- Inner ---------</p>");
            AddExceptionDetailsToMailBody(body, e.InnerException);
        }
    }
    #endregion
    public MailSender(string functionName, DateTime functionStartTime, ILogger logger)
    {
        this.functionName = functionName;
        this.logger = logger;
        var sendGridSender = Environment.GetEnvironmentVariable("SendGridSender");
        var sendGridUser = Environment.GetEnvironmentVariable("SendGridUser");
        SenderEmail = new EmailAddress(sendGridSender, sendGridUser);
        sendGridKey = Environment.GetEnvironmentVariable("SendGridKey");
        this.functionStartTime = functionStartTime;
    }
    public async Task SendFailureInfoMailAsync(Exception e)
    {
        var body = new StringBuilder();

        body.Append($"<h1>MemCheck function '{functionName}' failure</h1>");
        body.Append($"<p>Sent by Azure func '{functionName}' {GetAssemblyVersion()} running on {Environment.MachineName}, started on {functionStartTime}, mail constructed at {DateTime.UtcNow}</p>");

        AddExceptionDetailsToMailBody(body, e);

        await SendAsync("MemCheck Azure function failure", body.ToString(), SenderEmail.AsArray());
    }
    public async Task SendAsync(string subject, string body, IEnumerable<EmailAddress> to)
    {
        var msg = new SendGridMessage()
        {
            From = SenderEmail,
            Subject = subject,
            HtmlContent = body
        };

        to.ToList().ForEach(address => msg.AddTo(address.Email, address.Name));
        msg.AddTo(SenderEmail);
        msg.SetClickTracking(false, false);

        var sendGridClient = new SendGridClient(sendGridKey);
        var response = await sendGridClient.SendEmailAsync(msg);

        logger.LogInformation($"Mail sent, status code {response.StatusCode}");
        logger.LogInformation($"Response body: {await response.Body.ReadAsStringAsync()}");
    }
    public EmailAddress SenderEmail { get; }
    public static string GetAssemblyVersion()
    {
        return typeof(MailSender).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
    }
}
