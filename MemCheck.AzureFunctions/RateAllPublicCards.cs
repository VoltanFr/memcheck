//using System.Globalization;
//using System.Text;
//using System.Threading.Tasks;
//using MemCheck.Application.Users;
//using MemCheck.Database;
//using Microsoft.ApplicationInsights.Extensibility;
//using Microsoft.Azure.Functions.Worker;

//namespace MemCheck.AzureFunctions;

//public sealed class RateAllPublicCards : AbstractMemCheckAzureFunction
//{
//    public RateAllPublicCards(TelemetryConfiguration telemetryConfiguration, MemCheckDbContext memCheckDbContext, MemCheckUserManager userManager)
//        : base(telemetryConfiguration, memCheckDbContext, userManager)
//    {
//    }
//    [Function(nameof(RateAllPublicCards))]
//    public async Task Run([TimerTrigger(Constants.Cron_RateAllPublicCards)] TimerInfo timer, FunctionContext context)
//    {
//        await RunAsync(timer, context);
//    }
//    protected override async Task<RunResult> RunAndCreateReportMailMainPartAsync(string defaultMailSubject)
//    {
//        var rater = new Application.Ratings.RateAllPublicCards(NewCallContext());
//        var result = await rater.RunAsync(new Application.Ratings.RateAllPublicCards.Request(BotUserId));

//        var reportMailMainPart = new StringBuilder()
//            .Append("<p>")
//            .Append(CultureInfo.InvariantCulture, $"<li>Considered {result.PublicCardCount} public cards</li>")
//            .Append(CultureInfo.InvariantCulture, $"<li>Changed the bot's ratings for {result.ChangedRatingCount} cards</li>")
//            .Append("</p>");

//        var changeInfo = result.ChangedRatingCount switch
//        {
//            0 => "no change",
//            1 => "1 change",
//            _ => $"{result.ChangedRatingCount} changes",
//        };

//        var mailSubject = $"{defaultMailSubject} ({changeInfo})";

//        return new RunResult(mailSubject, reportMailMainPart);
//    }
//}
