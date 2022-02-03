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
        public void Run([TimerTrigger("0 */1 * * * *", RunOnStartup = true)] TimerInfo myTimer, ExecutionContext context, ILogger log)
        {
            log.LogInformation($"{nameof(Notifier)} Azure func starting at {DateTime.Now} on {Environment.MachineName}");
            log.LogInformation($"Entry assembly: {AssemblyServices.GetDisplayInfoForAssembly(Assembly.GetEntryAssembly())}");
            var memCheckAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.FullName!.StartsWith("MemCheck"));
            var memCheckAssemblyDescriptions = memCheckAssemblies.Select(a => AssemblyServices.GetDisplayInfoForAssembly(a)).OrderBy(a => a);
            foreach (string asm in memCheckAssemblyDescriptions)
                log.LogInformation($"Assembly: {asm}");

            telemetryClient.TrackEvent($"{nameof(Notifier)} Azure func");

            log.LogInformation($"Function '{nameof(Notifier)}' ending, {DateTime.Now}");
        }
    }
}



//            var config = new ConfigurationBuilder().SetBasePath(context.FunctionAppDirectory).AddEnvironmentVariables().Build();

//            string sendGridKey = config["SendGridKey"];
//            string sendGridSender = config["SendGridSender"];
//            string sendGridUser = config["SendGridUser"];

//            var client = new SendGridClient(sendGridKey);
//            var senderEmail = new EmailAddress(sendGridSender, sendGridUser);
//            var msg = new SendGridMessage()
//            {
//                From = senderEmail,
//                Subject = "Mail sent from Azure function",
//                HtmlContent = $"Time here: {DateTime.Now}, running on machine '{Environment.MachineName}', assembly: {executingAssemblyName}"
//            };
//            msg.AddTo(new EmailAddress("VoltanFr@gmail.com"));
//            msg.AddBcc(new EmailAddress(sendGridSender));
//            msg.SetClickTracking(false, false);

//            var response = await client.SendEmailAsync(msg);

//            log.LogInformation($"Mail sent, status code {response.StatusCode}");
//            log.LogInformation($"Response body: {await response.Body.ReadAsStringAsync()}");

//        }
//    }
//}
