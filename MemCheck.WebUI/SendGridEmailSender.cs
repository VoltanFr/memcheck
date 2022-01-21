using Microsoft.AspNetCore.Identity.UI.Services;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Threading.Tasks;

namespace MemCheck.WebUI
{
    public sealed record SendGridSettings(string SendGridUser, string SendGridKey, string SendGridSender);

    public class SendGridEmailSender : IEmailSender
    {
        #region fields
        private readonly SendGridSettings settings;
        #endregion
        public SendGridEmailSender(SendGridSettings settings)
        {
            this.settings = settings;
        }
        public async Task SendEmailAsync(string email, string subject, string message)
        {
            var client = new SendGridClient(settings.SendGridKey);
            var senderEmail = new EmailAddress(settings.SendGridSender, settings.SendGridUser);
            var msg = new SendGridMessage()
            {
                From = senderEmail,
                Subject = subject,
                PlainTextContent = message,
                HtmlContent = message,
            };
            msg.AddTo(new EmailAddress(email));
            msg.AddBcc(new EmailAddress(settings.SendGridSender));
            msg.SetClickTracking(false, false);
            await client.SendEmailAsync(msg);
        }
        public string Sender => settings.SendGridSender;
        public static string SenderFromInterface(IEmailSender emailSender)
        {
            var sendGrid = emailSender as SendGridEmailSender;
            return sendGrid != null ? sendGrid.Sender : "Unknown";
        }
    }
}
