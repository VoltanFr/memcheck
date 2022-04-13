using MemCheck.Basics;
using MemCheck.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace MemCheck.CommandLineDbClient.Images
{
    internal sealed class RecomputeAllSHA1 : ICmdLinePlugin
    {
        #region Fields
        private readonly ILogger<RecomputeAllSHA1> logger;
        private readonly MemCheckDbContext dbContext;
        #endregion
        public RecomputeAllSHA1(IServiceProvider serviceProvider)
        {
            dbContext = serviceProvider.GetRequiredService<MemCheckDbContext>();
            logger = serviceProvider.GetRequiredService<ILogger<RecomputeAllSHA1>>();
        }
        public void DescribeForOpportunityToCancel()
        {
            logger.LogInformation("Will recompute the SHA1 of all image blobs");
        }
        public async Task RunAsync()
        {
            var images = dbContext.Images;
            foreach (var image in images)
            {
                var originalBlobBytes = image.OriginalBlob;
                var sha1 = CryptoServices.GetSHA1(originalBlobBytes);
                image.OriginalBlobSha1 = sha1;
            }
            await dbContext.SaveChangesAsync();
        }
    }
}
