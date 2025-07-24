using System.Collections.Generic;
using System.Threading.Tasks;

namespace MemCheck.Domain;

//The redundancy of the name is because there is an interface IEMailSender in Microsoft.AspNetCore.Identity.UI.Services
public interface IMemCheckMailSender
{
    Task SendEmailAsync(MemCheckEmailAddress recipient, string subject, string htmlMessage);
    Task SendEmailAsync(IEnumerable<MemCheckEmailAddress> recipients, string subject, string htmlMessage);
    MemCheckEmailAddress SenderAddress { get; }
}
