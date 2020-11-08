using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace MemCheck.CommandLineDbClient.VolumeStress
{
    internal sealed class VolumeStressTest : IMemCheckTest
    {
        #region Fields
        private const int cardCount = 10000;
        private readonly ILogger<VolumeStressTest> logger;
        private readonly MemCheckDbContext dbContext;
        #endregion
        public VolumeStressTest(IServiceProvider serviceProvider)
        {
            dbContext = serviceProvider.GetService<MemCheckDbContext>();
            logger = serviceProvider.GetService<ILogger<VolumeStressTest>>();
        }
        async public Task RunAsync(MemCheckDbContext dbContext)
        {
            var cardLanguage = dbContext.CardLanguages.First();
            var user = dbContext.Users.First();

            //for (int i = 0; i < cardCount; i++)
            //{
            //    var card = new Card();
            //    card.Owner = null;
            //    card.CardLanguage = cardLanguage;
            //    card.FrontSide = "Bulk added card front side, " + Guid.NewGuid();
            //    card.BackSide = "Bulk added card back side, " + Guid.NewGuid();
            //    card.AdditionalInfo = "Bulk added additional info, " + Guid.NewGuid();
            //    card.CreationUtcDate = DateTime.Now.ToUniversalTime();
            //    card.LastChangeUtcDate = DateTime.Now.ToUniversalTime();
            //    card.UsersWithView = new List<Guid>() { user.Id };

            //    dbContext.Cards.Add(card);
            //}
            await dbContext.SaveChangesAsync();
        }
        public void DescribeForOpportunityToCancel()
        {
            logger.LogInformation($"DB contains {dbContext.Cards.Count()} cards");
            logger.LogInformation($"Bulk insertion of {cardCount} cards");
        }
    }
}
