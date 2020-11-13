using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Metrics;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace MemCheck.AzFunc.Notifier
{
    public sealed class SendStatsToAdmins
    {
        #region Fields
        private readonly TelemetryClient telemetryClient;
        #endregion
        public SendStatsToAdmins(TelemetryConfiguration telemetryConfiguration)
        {
            telemetryClient = new TelemetryClient(telemetryConfiguration);
        }
        [FunctionName(nameof(SendStatsToAdmins))]
        public async Task RunAsync([TimerTrigger("*/10 * * * *"
#if DEBUG
            , RunOnStartup=true
#endif
            )] TimerInfo myTimer, ExecutionContext context, ILogger log)
        {
            log.LogInformation($"Function '{nameof(SendStatsToAdmins)}' starting, {DateTime.Now} on {Environment.MachineName}");

            telemetryClient.TrackEvent($"{nameof(SendStatsToAdmins)} VinceTelemetryEvent");

            Assembly? assembly = Assembly.GetExecutingAssembly();
            var entryAssemblyName = assembly == null ? "Unknown" : (assembly.FullName == null ? "Unknown (no full name)" : assembly.FullName.ToString());
            log.LogInformation($"Assembly name: {entryAssemblyName}");

            var config = new ConfigurationBuilder().SetBasePath(context.FunctionAppDirectory).AddEnvironmentVariables().Build();

            string sendGridKey = config["SendGridKey"];
            string sendGridSender = config["SendGridSender"];
            string sendGridUser = config["SendGridUser"];

            var client = new SendGridClient(sendGridKey);
            var senderEmail = new EmailAddress(sendGridSender, sendGridUser);
            var msg = new SendGridMessage()
            {
                From = senderEmail,
                Subject = "Mail sent from Azure function",
                PlainTextContent = $"Time here: {DateTime.Now}, running on machine '{Environment.MachineName}'"
            };
            msg.AddTo(new EmailAddress("VoltanFr@gmail.com"));
            msg.AddBcc(new EmailAddress(sendGridSender));
            msg.SetClickTracking(false, false);

            var response = await client.SendEmailAsync(msg);

            log.LogInformation($"Mail sent, status code {response.StatusCode}");
            log.LogInformation($"Response body: {await response.Body.ReadAsStringAsync()}");

            log.LogInformation($"Function '{nameof(SendStatsToAdmins)}' ending, {DateTime.Now}");
        }
    }
}
