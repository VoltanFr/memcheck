﻿namespace MemCheck.AzureFunctions;

internal static class Constants
{
    public const string Cron_RateAllPublicCards = "0 0 1 * * *";
    public const string Cron_SendStatsToAdministrators = "0 0 2 * * *";
    public const string Cron_UpdateTagStats = "0 0 3 * * *";
    public const string Cron_MakeWikipediaLinksDesktop = "0 0 4 * * *";
    public const string Cron_SendNotifications = "0 0 5 * * *";
    //public const string CronEach5Min = "0 */5 * * * *";
}
