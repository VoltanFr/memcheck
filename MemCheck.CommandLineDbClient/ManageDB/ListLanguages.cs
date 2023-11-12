using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace MemCheck.CommandLineDbClient.ManageDB;

internal sealed class ListLanguages : ICmdLinePlugin
{
    #region Fields
    private readonly ILogger<ListUsers> logger;
    private readonly MemCheckDbContext dbContext;
    #endregion
    public ListLanguages(IServiceProvider serviceProvider)
    {
        dbContext = serviceProvider.GetRequiredService<MemCheckDbContext>();
        logger = serviceProvider.GetRequiredService<ILogger<ListUsers>>();
    }
    public void DescribeForOpportunityToCancel()
    {
        logger.LogInformation("Will list languages");
    }
    public async Task RunAsync()
    {
        var languages = await dbContext.CardLanguages.ToListAsync();

        foreach (var language in languages)
            logger.LogInformation($"Language '{language.Name}' has id {language.Id}");
    }
}
