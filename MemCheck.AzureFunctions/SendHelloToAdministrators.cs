//using System;
//using System.Threading.Tasks;
//using MemCheck.Application.Users;
//using MemCheck.Database;
//using Microsoft.ApplicationInsights.Extensibility;
//using Microsoft.Azure.WebJobs;
//using Microsoft.Extensions.Logging;

//namespace MemCheck.AzureFunctions;

//public sealed class SendHelloToAdministrators : AbstractMemCheckAzureFunction
//{
//    public SendHelloToAdministrators(TelemetryConfiguration telemetryConfiguration, MemCheckDbContext memCheckDbContext, MemCheckUserManager userManager, ILogger<SendHelloToAdministrators> logger)
//        : base(telemetryConfiguration, memCheckDbContext, userManager, logger)
//    {
//    }
//    [FunctionName(nameof(SendHelloToAdministrators))]
//    public async Task Run([TimerTrigger(Constants.CronEach5Min)] TimerInfo timer, ExecutionContext context)
//    {
//        await RunAsync(timer, context);
//    }
//    protected override async Task<string> RunAndCreateReportMailMainPartAsync()
//    {
//        await Task.CompletedTask;
//        return $"<h1>MemCheck says hello</h1><h2>Time here</h2><p>{DateTime.Now}</p>";
//    }
//}
