using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace MemCheck.AzFunc.Notifier
{
    public class AuthMessageSenderOptions
    {
        public string SendGridUser { get; set; } = null!;
        public string SendGridKey { get; set; } = null!;
        public string SendGridSender { get; set; } = null!;
    }

    public static class SendStatsToAdmins
    {
        [FunctionName("SendStatsToAdmins")] //Runs everyday at 3 AM
        public static async Task RunAsync([TimerTrigger("0 3 * * *"
#if DEBUG
            , RunOnStartup=true
#endif
            )] TimerInfo myTimer, ExecutionContext context, ILogger log)
        {
            log.LogInformation($"Function '{nameof(SendStatsToAdmins)}' starting, {DateTime.Now}");

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
                PlainTextContent = "Time here: " + DateTime.Now,
                HtmlContent = "Time here: " + DateTime.Now
            };
            msg.AddTo(new EmailAddress("MemCheckAdm@gmail.com"));
            msg.AddBcc(new EmailAddress(sendGridSender));

            // Disable click tracking.
            // See https://sendgrid.com/docs/User_Guide/Settings/tracking.html
            msg.SetClickTracking(false, false);

            var response = await client.SendEmailAsync(msg);

            log.LogInformation($"Mail sent, status code {response.StatusCode}");
            log.LogInformation($"Response body: {await response.Body.ReadAsStringAsync()}");

            log.LogInformation($"Function '{nameof(SendStatsToAdmins)}' ending, {DateTime.Now}");
        }
    }
}
