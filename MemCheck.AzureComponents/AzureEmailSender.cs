using Azure.Communication.Email;
using Azure;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace MemCheck.AzureComponents;

public sealed class AzureEmailSender : IEmailSender
{
    private readonly EmailClient emailClient;
    public AzureEmailSender(string connectionString)
    {
        emailClient = new EmailClient(connectionString);
    }
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var emailMessage = new EmailMessage(
            senderAddress: "DoNotReply@mnesios.com",
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
}
