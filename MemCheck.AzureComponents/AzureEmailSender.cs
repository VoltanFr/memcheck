using Azure.Communication.Email;
using Azure;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MemCheck.AzureComponents;

//The redundancy of the name is because there is an interface IEMailSender in Microsoft.AspNetCore.Identity.UI.Services
public interface IMemCheckEmailSender
{
    Task SendEmailAsync(string email, string subject, string htmlMessage);
    string SenderAddress { get; }
}

public sealed class AzureEmailSender : IMemCheckEmailSender
{
    private readonly EmailClient emailClient;
    public AzureEmailSender(string connectionString)
    {
        emailClient = new EmailClient(connectionString);
    }
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var emailMessage = new EmailMessage(
            senderAddress: SenderAddress,
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
    public string SenderAddress => "DoNotReply@mnesios.com";
}
