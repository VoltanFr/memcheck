using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MemCheck.Basics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace MemCheck.AzureFunctions;

public sealed class MailSender
{
    #region Fields
    private readonly ILogger logger;
    private readonly DateTime functionStartTime;
    private readonly string sendGridKey;
    #endregion
    #region Private methods
    private void AddExceptionDetailsToMailBody(StringBuilder body, Exception e)
    {
        body = body
            .Append(CultureInfo.InvariantCulture, $"<p>Caught {e.GetType().Name}</p>")
            .Append(CultureInfo.InvariantCulture, $"<p>Message: {e.Message}</p>");

        if (e.StackTrace != null)
            body = body.Append(CultureInfo.InvariantCulture, $"<p>Call stack: {e.StackTrace.Replace("\n", "<br/>", StringComparison.Ordinal)}</p>");

        if (e.InnerException != null)
        {
            body = body.Append(CultureInfo.InvariantCulture, $"<p>-------- Inner ---------</p>");
            AddExceptionDetailsToMailBody(body, e.InnerException);
        }
    }
    #endregion
    public MailSender(DateTime functionStartTime, ILogger logger)
    {
        this.logger = logger;
        var sendGridSender = Environment.GetEnvironmentVariable("SendGridSender");
        var sendGridUser = Environment.GetEnvironmentVariable("SendGridUser");
        SenderEmail = new EmailAddress(sendGridSender, sendGridUser);
        sendGridKey = Environment.GetEnvironmentVariable("SendGridKey") ?? "NoSendGridKey";
        this.functionStartTime = functionStartTime;
    }
    public async Task SendFailureInfoMailAsync(string functionName, Exception e)
    {
        var body = new StringBuilder()
            .Append(CultureInfo.InvariantCulture, $"<h1>Mnesios function '{functionName}' failure</h1>")
            .Append(CultureInfo.InvariantCulture, $"<p>Sent by Azure func '{functionName}' {GetAssemblyVersion()} running on {Environment.MachineName}, started on {functionStartTime}, mail constructed at {DateTime.UtcNow}</p>");

        AddExceptionDetailsToMailBody(body, e);

        await SendAsync("Mnesios Azure function failure", body.ToString(), SenderEmail.AsArray());
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

        logger.LogInformation("Mail sent, status code {StatusCode}", response.StatusCode);
        logger.LogInformation("Response body: {ResponseBody}", await response.Body.ReadAsStringAsync());
    }
    public EmailAddress SenderEmail { get; }
    public static string GetAssemblyVersion()
    {
        return typeof(MailSender).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
    }
    public static string GetMailFooter(string azureFunctionName, TimerInfo timer, DateTime azureFunctionStartTime, ImmutableList<EmailAddress> admins)
    {
        var listItems = new List<string> {
            $"<li>Sent by Azure func {azureFunctionName}</li>",
            $"<li>Running on {Environment.MachineName}, process id: {Environment.ProcessId}, process name: {Process.GetCurrentProcess().ProcessName}, peak mem usage: {ProcessServices.GetPeakProcessMemoryUsage()} bytes</li>",
            $"<li>Started on {azureFunctionStartTime}, mail constructed at {DateTime.UtcNow} (Elapsed: {DateTime.UtcNow-azureFunctionStartTime})</li>",
            $"<li>Function last schedule: {timer.ScheduleStatus?.Last}</li>",
            $"<li>Function last schedule updated: {timer.ScheduleStatus?.LastUpdated}</li>",
            $"<li>Function next schedule: {timer.ScheduleStatus?.Next}</li>",
            $"<li>Sent to {admins.Count} admins: {string.Join(",", admins.Select(a => a.Name))}</li>"
        };

        var writer = new StringBuilder()
            .Append("<h2>Info</h2>")
            .Append(CultureInfo.InvariantCulture, $"<ul>{string.Join("", listItems)}</ul>");

        return writer.ToString();
    }
}
