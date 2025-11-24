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
    private static readonly Action<ILogger, string, Exception?> azureMailConnectionLogMessage = LoggerMessage.Define<string>(LogLevel.Warning, new EventId(2, "AzureMailConnection"), "Using AzureMailConnection {Info}");
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
    private static string GetAzureMailConnectionString(IConfiguration configuration, bool prodEnvironment, ILogger logger)
    {
        if (prodEnvironment)
        {
            azureMailConnectionLogMessage(logger, "prod settings from app settings", null);
            return configuration.RequiredValue("AzureMailConnectionString");
        }
        var debuggingDb = configuration.RequiredValue("ConnectionStrings:DebuggingDb");
        if (debuggingDb is "LocalReplicatedFromProd" or "Azure")
        {
            var secrets = JsonSerializer.Deserialize<string>(File.ReadAllText(@"C:\BackedUp\DocsBV\Synchronized\SkyDrive\Programmation\MemCheck-private-info\AzureMailConnectionString.json"));
            azureMailConnectionLogMessage(logger, "prod settings from private file", null);
            if (secrets == null)
                throw new InvalidProgramException("Failed to load mail connection string");
            return secrets;
        }
        azureMailConnectionLogMessage(logger, "debug settings", null);
        return configuration.RequiredValue("AzureMailConnectionString");
    }
    #endregion
    public AppSettings(IConfiguration configuration, bool prodEnvironment, ILogger<AppSettings> logger)
    {
        ConnectionString = GetConnectionString(configuration, prodEnvironment, logger);
        AzureMailConnectionString = GetAzureMailConnectionString(configuration, prodEnvironment, logger);
        RecipientToAddInBccOfAllMails = configuration.RequiredValue("RecipientToAddInBccOfAllMails");
        TurnstileSiteKey = configuration.RequiredValue("Turnstile:SiteKey");
        TurnstileSecretKey = configuration.RequiredValue("Turnstile:SecretKey");
    }
    public string ConnectionString { get; }
    public string AzureMailConnectionString { get; }
    public string RecipientToAddInBccOfAllMails { get; }
    public string TurnstileSiteKey { get; }
    public string TurnstileSecretKey { get; }
}
