using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Threading.Tasks;

namespace MemCheck.WebUI
{
    public class AuthMessageSenderOptions
    {
        public string SendGridUser { get; set; } = null!;
        public string SendGridKey { get; set; } = null!;
        public string SendGridSender { get; set; } = null!;
    }

    public class SendGridEmailSender : IEmailSender
    {
        #region fields
        private readonly AuthMessageSenderOptions options;
        #endregion
        public SendGridEmailSender(IOptions<AuthMessageSenderOptions> optionsAccessor)
        {
            options = optionsAccessor.Value;
        }
        public async Task SendEmailAsync(string email, string subject, string message)
        {
            var client = new SendGridClient(options.SendGridKey);
            var senderEmail = new EmailAddress(options.SendGridSender, options.SendGridUser);
            var msg = new SendGridMessage()
            {
                From = senderEmail,
                Subject = subject,
                PlainTextContent = message,
                HtmlContent = message,
            };
            msg.AddTo(new EmailAddress(email));
            msg.AddBcc(new EmailAddress(options.SendGridSender));
            msg.SetClickTracking(false, false);
            await client.SendEmailAsync(msg);
        }
    }
}
