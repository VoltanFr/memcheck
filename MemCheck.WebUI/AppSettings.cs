syntax error
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.Json;

namespace MemCheck.WebUI
{
    public sealed class AppSettings
    {
        #region Private methods
        private static string GetConnectionString(IConfiguration configuration, bool prodEnvironment, ILogger logger)
        {
            if (prodEnvironment)
            {
                logger.LogInformation("Using prod DB from app settings");
                return configuration["ConnectionStrings:AzureDbConnection"];
            }
            var debuggingDb = configuration["ConnectionStrings:DebuggingDb"];
            if (debuggingDb == "Azure")
            {
                logger.LogWarning("Using Azure prod DB from private file");
                return File.ReadAllText(@"C:\BackedUp\DocsBV\Synchronized\SkyDrive\Programmation\MemCheck private info\AzureConnectionString.txt").Trim();
            }
            logger.LogInformation($"Using DB '{debuggingDb}'");
            return configuration[$"ConnectionStrings:{debuggingDb}"];
        }
        private static SendGridSettings GetSendGrid(IConfiguration configuration, bool prodEnvironment, ILogger logger)
        {
            if (prodEnvironment)
            {
                logger.LogInformation("Using prod SendGrid settings from app settings");
                return new SendGridSettings(configuration["SendGrid:User"], configuration["SendGrid:Key"], configuration["SendGrid:Sender"]);
            }
            var debuggingDb = configuration["ConnectionStrings:DebuggingDb"];
            if (debuggingDb == "AlternativeProdDbConnection" || debuggingDb == "Azure")
            {
                var secrets = JsonSerializer.Deserialize<SendGridSettings>(File.ReadAllText(@"C:\BackedUp\DocsBV\Synchronized\SkyDrive\Programmation\MemCheck private info\SendGridSecrets.json"));
                logger.LogWarning("Using prod SendGrid settings from private file");
                return new SendGridSettings(secrets!.SendGridUser, secrets.SendGridKey, secrets.SendGridSender);
            }
            logger.LogInformation($"Using debug SendGrid settings");
            return new SendGridSettings(configuration["SendGrid:User"], configuration["SendGrid:Key"], configuration["SendGrid:Sender"]);
        }
        #endregion
        public AppSettings(IConfiguration configuration, bool prodEnvironment, ILogger<AppSettings> logger)
        {
            ConnectionString = GetConnectionString(configuration, prodEnvironment, logger);
            SendGrid = GetSendGrid(configuration, prodEnvironment, logger);
        }
        public SendGridSettings SendGrid { get; }
        public string ConnectionString { get; }
    }
}
