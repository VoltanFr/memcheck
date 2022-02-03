using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace MemCheck.Notifier.AzureFunc
{    
    public class Notifier
    {
        [FunctionName("Notifier")]
        public void Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"Vince C# Timer trigger function executed at: {DateTime.Now}");
        }
    }
}
