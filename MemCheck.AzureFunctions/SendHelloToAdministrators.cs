//using System;
//using System.Text;
//using System.Threading.Tasks;
//using MemCheck.Application.Users;
//using MemCheck.Database;
//using Microsoft.ApplicationInsights.Extensibility;
//using Microsoft.Azure.Functions.Worker;
//using Microsoft.Extensions.Logging;

//namespace MemCheck.AzureFunctions;

//public sealed class SendHelloToAdministrators : AbstractMemCheckAzureFunction
//{
//    public SendHelloToAdministrators(TelemetryConfiguration telemetryConfiguration, MemCheckDbContext memCheckDbContext, MemCheckUserManager userManager, ILogger<SendHelloToAdministrators> logger)
//        : base(telemetryConfiguration, memCheckDbContext, userManager, logger)
//    {
//    }
//    [Function(nameof(SendHelloToAdministrators))]
//    public async Task Run([TimerTrigger(Constants.CronEachMin)] TimerInfo timer, FunctionContext context)
//    {
//        await RunAsync(timer, context);
//    }
//    protected override async Task<RunResult> RunAndCreateReportMailMainPartAsync(string defaultMailSubject)
//    {
//        await Task.CompletedTask;
//        var body = new StringBuilder($"<h1>MemCheck says hello</h1><h2>Time here</h2><p>{DateTime.Now}</p>");
//        return new RunResult(defaultMailSubject, body);
//    }
//}
