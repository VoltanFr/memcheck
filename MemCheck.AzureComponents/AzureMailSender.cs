using Azure.Communication.Email;
using Azure;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MemCheck.Domain;
using MemCheck.Basics;
using System.Linq;

namespace MemCheck.AzureComponents;

public sealed class AzureMailSender : IMemCheckMailSender
{
    #region Fields
    private readonly EmailClient emailClient;
    private readonly EmailAddress recipientToAddInBccOfAllMails;
    #endregion
    public AzureMailSender(string connectionString, string recipientToAddInBccOfAllMails)
    {
        emailClient = new EmailClient(connectionString);
        this.recipientToAddInBccOfAllMails = new EmailAddress(recipientToAddInBccOfAllMails);
    }
    public async Task SendAsync(MemCheckEmailAddress recipient, string subject, string htmlMessage)
    {
        await SendAsync(recipient.AsArray(), subject, htmlMessage).ConfigureAwait(false);
    }
    public async Task SendAsync(IEnumerable<MemCheckEmailAddress> recipients, string subject, string htmlMessage)
    {
        var content = new EmailContent(subject)
        {
            PlainText = htmlMessage,
            Html = htmlMessage
        };
        var recipientAddresses = new EmailRecipients(recipients.Select(r => new EmailAddress(r.Address, r.DisplayName)), bcc: recipientToAddInBccOfAllMails.AsArray());
        var emailMessage = new EmailMessage(SenderAddress.Address, recipientAddresses, content);
        var emailSendOperation = await emailClient.SendAsync(WaitUntil.Completed, emailMessage);

        if (!emailSendOperation.HasCompleted)
            Console.WriteLine("Mail sending failed");
    }
    public MemCheckEmailAddress SenderAddress => new("DoNotReply@mnesios.com", "Don't reply");
}
