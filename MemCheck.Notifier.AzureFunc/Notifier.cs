using System;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Reflection;
using MemCheck.Basics;
using System.Linq;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MemCheck.Notifier.AzureFunc
{
    public class Notifier
    {
        #region Fields
        private readonly TelemetryClient telemetryClient;
        #endregion
        public Notifier(TelemetryConfiguration telemetryConfiguration)
        {
            telemetryClient = new TelemetryClient(telemetryConfiguration);
        }
        [FunctionName(nameof(Notifier))]
        public async Task Run([TimerTrigger("0 */1 * * * *", RunOnStartup = true)] TimerInfo myTimer, ExecutionContext context, ILogger log)
        {
            telemetryClient.TrackEvent($"{nameof(Notifier)} Azure func start");

            var mailLines = new List<string>();

            mailLines.Add($"{nameof(Notifier)} Azure func starting at {DateTime.Now} on {Environment.MachineName}");
            mailLines.Add($"Entry assembly: {AssemblyServices.GetDisplayInfoForAssembly(Assembly.GetEntryAssembly())}");
            var memCheckAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.FullName!.StartsWith("MemCheck"));
            var memCheckAssemblyDescriptions = memCheckAssemblies.Select(a => AssemblyServices.GetDisplayInfoForAssembly(a)).OrderBy(a => a);
            foreach (string asm in memCheckAssemblyDescriptions)
                mailLines.Add($"Assembly: {asm}");

            foreach (string mailLine in mailLines)
                log.LogInformation(mailLine);

            var config = new ConfigurationBuilder().SetBasePath(context.FunctionAppDirectory).AddEnvironmentVariables().Build();

            var sendGridKey = config["SendGridKey"];
            var sendGridSender = config["SendGridSender"];
            var sendGridUser = config["SendGridUser"];

            log.LogInformation($"SendGridKey is {(sendGridKey == "ThisIsSecret" ? "NOT " : "")} set");

            var sendgridClient = new SendGridClient(sendGridKey);
            var senderEmail = new EmailAddress(sendGridSender, sendGridUser);
            var msg = new SendGridMessage()
            {
                From = senderEmail,
                Subject = $"Mail sent from {nameof(Notifier)} Azure func",
                HtmlContent = string.Join("<br/>", mailLines)
            };
            msg.AddTo(new EmailAddress("VoltanFr@gmail.com"));
            msg.AddBcc(new EmailAddress(sendGridSender));
            msg.SetClickTracking(false, false);

            var response = await sendgridClient.SendEmailAsync(msg);

            log.LogInformation($"Mail sent, status code {response.StatusCode}");
            log.LogInformation($"Response body: {await response.Body.ReadAsStringAsync()}");

            log.LogInformation($"Function '{nameof(Notifier)}' ending, {DateTime.Now}");
        }
    }
}
