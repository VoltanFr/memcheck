using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MemCheck.Basics;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace MemCheck.AzureFunctions
{
    public class SendStatsToAdministrators
    {
        #region Fields
        public const string FunctionName = nameof(SendStatsToAdministrators);
        private readonly TelemetryClient telemetryClient;
        #endregion
        public SendStatsToAdministrators(TelemetryConfiguration telemetryConfiguration)
        {
            telemetryClient = new TelemetryClient(telemetryConfiguration);
        }
        [FunctionName(FunctionName)]
        public async Task Run([TimerTrigger("0 */1 * * * *", RunOnStartup = true)] TimerInfo myTimer, ExecutionContext context, ILogger log)
        {
            telemetryClient.TrackEvent($"{FunctionName} Azure func start");

            var mailLines = new List<string>();

            mailLines.Add($"{FunctionName} Azure func starting at {DateTime.Now} on {Environment.MachineName}");
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

            if (sendGridKey == "ThisIsSecret")
                log.LogInformation("SendGridKey is not set");
            else
            if (sendGridKey == null)
                log.LogInformation("SendGridKey is null");
            else
                log.LogInformation("SendGridKey is set");

            var sendgridClient = new SendGridClient(sendGridKey);
            var senderEmail = new EmailAddress(sendGridSender, sendGridUser);
            var msg = new SendGridMessage()
            {
                From = senderEmail,
                Subject = $"Mail sent from {FunctionName} Azure func",
                HtmlContent = string.Join("<br/>", mailLines)
            };
            msg.AddTo(new EmailAddress("VoltanFr@gmail.com"));
            msg.AddBcc(new EmailAddress(sendGridSender));
            msg.SetClickTracking(false, false);

            var response = await sendgridClient.SendEmailAsync(msg);

            log.LogInformation($"Mail sent, status code {response.StatusCode}");
            log.LogInformation($"Response body: {await response.Body.ReadAsStringAsync()}");

            log.LogInformation($"Function '{FunctionName}' ending, {DateTime.Now}");
        }
    }
}
