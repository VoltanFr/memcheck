using MemCheck.Application;
using MemCheck.Application.CardChanging;
using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemCheck.CommandLineDbClient.Deletion
{
    internal sealed class BrutalDeletion : IMemCheckTest
    {
        #region Fields
        private readonly ILogger<BrutalDeletion> logger;
        private readonly MemCheckDbContext dbContext;
        #endregion
        public BrutalDeletion(IServiceProvider serviceProvider)
        {
            dbContext = serviceProvider.GetRequiredService<MemCheckDbContext>();
            logger = serviceProvider.GetRequiredService<ILogger<BrutalDeletion>>();
        }
        public void DescribeForOpportunityToCancel()
        {
            logger.LogInformation($"Will delete some cards - D A N G E R");
        }
        async public Task RunAsync(MemCheckDbContext dbContext)
        {
            var cards = await dbContext.Cards.Where(c => (c.VersionCreator.UserName == "Toto2" || c.VersionCreator.UserName == "Voltan" || c.VersionCreator.UserName == "VoltanBot") && c.TagsInCards.Count() == 1 && c.TagsInCards.First().Tag.Name == "États américains").ToListAsync();

            foreach (var card in cards)
                logger.LogInformation($"\t {card.FrontSide.Substring(0, Math.Min(100, card.FrontSide.Length))}");
            logger.LogInformation($"{cards.Count()} cards selected");
            logger.LogWarning("Opportunity to cancel. Please confirm with Y");
            var input = Console.ReadLine();
            if (input == null || !input.Trim().Equals("y", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogError("User cancellation");
                return;
            }

            dbContext.Cards.RemoveRange(cards);

            //var user = dbContext.Users.Where(u => u.UserName == "VoltanBot").Single();//
            //var frenchLanguageId = dbContext.CardLanguages.Where(lang => lang.Name == "Français").Select(lang => lang.Id).Single();
            //var tagId = dbContext.Tags.Where(tag => tag.Name == "États américains").Select(tag => tag.Id).Single();
            //var statesWithNamesImageId = dbContext.Images.Where(img => img.Name == "Carte.Amérique.USA.États").Select(img => img.Id).Single();
            //var statesWithoutNamesImageId = dbContext.Images.Where(img => img.Name == "États américains sans noms").Select(img => img.Id).Single();

            //foreach (var state in infoFileContents.States)
            //{
            //    logger.LogDebug($"Working on state '{state.FrenchName}'");

            //    await CreateCard_WhatIsTheNameOfThisStateAsync(state, user, statesWithNamesImageId, frenchLanguageId, tagId);
            //    await CreateCard_WhereIsThisStateAsync(state, user, statesWithoutNamesImageId, frenchLanguageId, tagId);
            //    await CreateCard_WhatIsTheCapitalOfAsync(state, user, frenchLanguageId, tagId);
            //    await CreateCard_WhatIsTheStateOfThisCapitalAsync(state, user, frenchLanguageId, tagId);
            //}

            await dbContext.SaveChangesAsync();
            logger.LogInformation($"Deletion finished");
        }
    }
}
