using Azure.Communication.Email;
using Azure;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MemCheck.Domain;

namespace MemCheck.AzureComponents;

//The redundancy of the name is because there is an interface IEMailSender in Microsoft.AspNetCore.Identity.UI.Services
public interface IMemCheckMailSender
{
    Task SendEmailAsync(MemCheckEmailAddress recipient, string subject, string htmlMessage);
    MemCheckEmailAddress SenderAddress { get; }
}

public sealed class AzureMailSender : IMemCheckMailSender
{
    #region Fields
    private readonly EmailClient emailClient;
    #endregion
    public AzureMailSender(string connectionString)
    {
        emailClient = new EmailClient(connectionString);
    }
    public async Task SendEmailAsync(MemCheckEmailAddress recipient, string subject, string htmlMessage)
    {
        var content = new EmailContent(subject)
        {
            PlainText = htmlMessage,
            Html = htmlMessage
        };
        var recipients = new EmailRecipients(new List<EmailAddress> { new(recipient.Address, recipient.DisplayName) });
        var emailMessage = new EmailMessage(senderAddress: SenderAddress.Address, recipients, content);
        var emailSendOperation = await emailClient.SendAsync(WaitUntil.Completed, emailMessage);

        if (!emailSendOperation.HasCompleted)
            Console.WriteLine("Mail sending failed");
    }
    public MemCheckEmailAddress SenderAddress => new("DoNotReply@mnesios.com", "Don't reply");
}
