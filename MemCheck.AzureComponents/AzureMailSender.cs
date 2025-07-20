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
    Task SendEmailAsync(string email, string subject, string htmlMessage);
    MemCheckEmailAddress SenderAddress { get; }
}

public sealed class AzureMailSender : IMemCheckMailSender
{
    private readonly EmailClient emailClient;
    public AzureMailSender(string connectionString)
    {
        emailClient = new EmailClient(connectionString);
    }
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var emailMessage = new EmailMessage(
            senderAddress: SenderAddress.Address,
            content: new EmailContent(subject)
            {
                PlainText = htmlMessage,
                Html = htmlMessage
            },
            recipients: new EmailRecipients(new List<EmailAddress> { new(email) }));


        var emailSendOperation = await emailClient.SendAsync(WaitUntil.Completed, emailMessage);

        if (!emailSendOperation.HasCompleted)
        {
            Console.WriteLine("Mail sending failed");
        }
    }
    public MemCheckEmailAddress SenderAddress => new("DoNotReply@mnesios.com", "Don't reply");
}
