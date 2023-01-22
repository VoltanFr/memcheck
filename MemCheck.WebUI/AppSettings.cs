using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text.Json;

namespace MemCheck.WebUI;

public static class IConfigurationExtensions
{
    public static string RequiredValue(this IConfiguration configuration, string valueName)
    {
        var result = configuration[valueName];
        return result ?? throw new InvalidOperationException($"Configuration does not contain a value for {valueName}");
    }
}

public sealed class AppSettings
{
    #region Fields
    private static readonly Action<ILogger, string, Exception?> dbInfoLogMessage = LoggerMessage.Define<string>(LogLevel.Warning, new EventId(1, "DbInfo"), "Using {DbInfo}");
    private static readonly Action<ILogger, string, Exception?> sendGridLogMessage = LoggerMessage.Define<string>(LogLevel.Warning, new EventId(2, "SendGrid"), "Using SendGrid {Info}");
    #endregion
    #region Private methods
    private static string GetConnectionString(IConfiguration configuration, bool prodEnvironment, ILogger logger)
    {
        if (prodEnvironment)
        {
            dbInfoLogMessage(logger, "prod DB from app settings", null);
            return configuration.RequiredValue("ConnectionStrings:AzureDbConnection");
        }
        var debuggingDb = configuration.RequiredValue("ConnectionStrings:DebuggingDb");
        if (debuggingDb == "Azure")
        {
            dbInfoLogMessage(logger, "Azure prod DB from private file", null);
            return File.ReadAllText(@"C:\BackedUp\DocsBV\Synchronized\SkyDrive\Programmation\MemCheck-private-info\AzureConnectionString.txt").Trim();
        }
        dbInfoLogMessage(logger, $"DB '{debuggingDb}'", null);
        return configuration.RequiredValue($"ConnectionStrings:{debuggingDb}");
    }
    private static SendGridSettings GetSendGrid(IConfiguration configuration, bool prodEnvironment, ILogger logger)
    {
        if (prodEnvironment)
        {
            sendGridLogMessage(logger, "prod settings from app settings", null);
            return new SendGridSettings(configuration.RequiredValue("SendGrid:User"), configuration.RequiredValue("SendGrid:Key"), configuration.RequiredValue("SendGrid:Sender"));
        }
        var debuggingDb = configuration.RequiredValue("ConnectionStrings:DebuggingDb");
        if (debuggingDb is "AlternativeProdDbConnection" or "Azure")
        {
            var secrets = JsonSerializer.Deserialize<SendGridSettings>(File.ReadAllText(@"C:\BackedUp\DocsBV\Synchronized\SkyDrive\Programmation\MemCheck-private-info\SendGridSecrets.json"));
            sendGridLogMessage(logger, "prod settings from private file", null);
            return new SendGridSettings(secrets!.SendGridUser, secrets.SendGridKey, secrets.SendGridSender);
        }
        sendGridLogMessage(logger, "debug settings", null);
        return new SendGridSettings(configuration.RequiredValue("SendGrid:User"), configuration.RequiredValue("SendGrid:Key"), configuration.RequiredValue("SendGrid:Sender"));
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
