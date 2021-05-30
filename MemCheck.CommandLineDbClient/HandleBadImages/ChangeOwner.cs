using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.CommandLineDbClient.HandleBadImages
{
    internal sealed class ChangeOwner : ICmdLinePlugin
    {
        #region Fields
        private readonly ILogger<ChangeOwner> logger;
        private readonly MemCheckDbContext dbContext;
        #endregion
        public ChangeOwner(IServiceProvider serviceProvider)
        {
            dbContext = serviceProvider.GetRequiredService<MemCheckDbContext>();
            logger = serviceProvider.GetRequiredService<ILogger<ChangeOwner>>();
        }
        public void DescribeForOpportunityToCancel()
        {
            logger.LogInformation("Will change owners previous image versions with null ones");
        }
        async public Task RunAsync()
        {
            var user = dbContext.Users.Where(user => user.UserName == "Voltan").Single();

            logger.LogInformation($"DB contains {dbContext.ImagePreviousVersions.Count()} image versions");

            var images = dbContext.ImagePreviousVersions.Include(img => img.Owner).Where(image => image.Owner == null);
            logger.LogInformation($"Found {images.Count()} image versions to modify");
            foreach (var image in images)
            {
                logger.LogInformation($"Changing image '{image.Name}'");
                image.Owner = user;
            }

            await dbContext.SaveChangesAsync();
            logger.LogInformation($"Finished");
        }
    }
}
