using System;
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
        public static async Task RunAsync([TimerTrigger("0 3 * * *")] TimerInfo myTimer, ExecutionContext context, ILogger log)
        {
            log.LogInformation($"Function '{nameof(SendStatsToAdmins)}' starting, {DateTime.Now}");

            var config = new ConfigurationBuilder()
  .SetBasePath(context.FunctionAppDirectory)
  //.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
  // This is what actually gets you the application settings in Azure
  .AddEnvironmentVariables()
  .Build();

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

            await client.SendEmailAsync(msg);

        }
    }
}
