using MemCheck.CommandLineDbClient.Geography;
using MemCheck.CommandLineDbClient.Pauker;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.CommandLineDbClient.RegionsFrancaises
{
    internal sealed class RegionsCardImprovementToMarkdownAndDepartmentNumbers : ICmdLinePlugin
    {
        #region Fields
        private readonly ILogger<PaukerImportTest> logger;
        private readonly MemCheckDbContext dbContext;
        #endregion
        #region Private methods
        private async Task UpdateCards_WhatIsThisRegionAsync(Guid userId, GeoInfo geoInfo)
        {
            var cardsToUpdate = await dbContext.Cards
                .Where(card => card.VersionCreator.Id == userId && card.FrontSide == "Comment s'appelle cette région française ?")
            .ToListAsync();

            foreach (var cardToUpdate in cardsToUpdate)
            {
                var additionalInfoLines = cardToUpdate.AdditionalInfo.Split(Environment.NewLine).Select(line => line.Trim()).ToList();
                if (!additionalInfoLines[0].StartsWith("Elle est constituée de ces"))
                    throw new Exception($"Unexpected card additional info first line: '{additionalInfoLines[0]}'");
                if (additionalInfoLines[1].StartsWith("-"))
                    logger.LogInformation($"Card is already Markdown ({cardToUpdate.BackSide})");
                else
                {
                    var lineIndex = 1;
                    while (!string.IsNullOrEmpty(additionalInfoLines[lineIndex]))
                    {
                        var department = geoInfo.DepartmentsFromName[additionalInfoLines[lineIndex]];
                        additionalInfoLines[lineIndex] = $"- {department.FullName} ({department.DepartmentCode})";
                        lineIndex++;
                    }
                    var result = string.Join(Environment.NewLine, additionalInfoLines);
                    cardToUpdate.AdditionalInfo = result.Trim();
                }
            }
        }
        private async Task UpdateCards_WhereIsThisRegionAsync(Guid userId, GeoInfo geoInfo)
        {
            var cardsToUpdate = await dbContext.Cards
                .Where(card => card.VersionCreator.Id == userId && card.FrontSide.StartsWith("Où est la région"))
                .ToListAsync();

            foreach (var cardToUpdate in cardsToUpdate)
            {
                var additionalInfoLines = cardToUpdate.AdditionalInfo.Split(Environment.NewLine).Select(line => line.Trim()).ToList();
                if (!additionalInfoLines[0].StartsWith("Elle est constituée de ces"))
                    throw new Exception($"Unexpected card additional info first line: '{additionalInfoLines[0]}'");
                if (additionalInfoLines[1].StartsWith("-"))
                    logger.LogInformation($"Card is already Markdown ({cardToUpdate.BackSide})");
                else
                {
                    var lineIndex = 1;
                    while (!string.IsNullOrEmpty(additionalInfoLines[lineIndex]))
                    {
                        var department = geoInfo.DepartmentsFromName[additionalInfoLines[lineIndex]];
                        additionalInfoLines[lineIndex] = $"- {department.FullName} ({department.DepartmentCode})";
                        lineIndex++;
                    }
                    var result = string.Join(Environment.NewLine, additionalInfoLines);
                    cardToUpdate.AdditionalInfo = result.Trim();
                }
            }
        }
        private async Task UpdateCards_HowManyDepartmentsInThisRegionAsync(Guid userId, GeoInfo geoInfo)
        {
            var cardsToUpdate = await dbContext.Cards
                .Where(card => card.VersionCreator.Id == userId && card.FrontSide.StartsWith("Combien y a-t-il de départements dans la région"))
                .ToListAsync();

            foreach (var cardToUpdate in cardsToUpdate)
            {
                var additionalInfoLines = cardToUpdate.AdditionalInfo.Split(Environment.NewLine).Select(line => line.Trim()).ToList();
                if (additionalInfoLines[0].StartsWith("-"))
                    logger.LogInformation($"Card is already Markdown ({cardToUpdate.FrontSide})");
                else
                {
                    for (int lineIndex = 0; lineIndex < additionalInfoLines.Count; lineIndex++)
                    {
                        var department = geoInfo.DepartmentsFromName[additionalInfoLines[lineIndex]];
                        additionalInfoLines[lineIndex] = $"- {department.FullName} ({department.DepartmentCode})";
                    }
                    var result = string.Join(Environment.NewLine, additionalInfoLines);
                    cardToUpdate.AdditionalInfo = result.Trim();
                }
            }
        }
        private async Task UpdateCards_WhatAreTheDepartmentsInThisRegionAsync(Guid userId, GeoInfo geoInfo)
        {
            var cardsToUpdate = await dbContext.Cards
                .Where(card => card.VersionCreator.Id == userId && card.FrontSide.StartsWith("Quels départements y a-t-il dans la région"))
                .ToListAsync();

            foreach (var cardToUpdate in cardsToUpdate)
            {
                var backSideLines = cardToUpdate.BackSide.Split(Environment.NewLine).Select(line => line.Trim()).ToList();
                if (backSideLines[0].StartsWith("-"))
                    logger.LogInformation($"Card is already Markdown ({cardToUpdate.FrontSide})");
                else
                {
                    for (int lineIndex = 0; lineIndex < backSideLines.Count; lineIndex++)
                    {
                        var department = geoInfo.DepartmentsFromName[backSideLines[lineIndex]];
                        backSideLines[lineIndex] = $"- {department.FullName} ({department.DepartmentCode})";
                    }
                    var result = string.Join(Environment.NewLine, backSideLines);
                    cardToUpdate.BackSide = result.Trim();
                }
            }
        }
        #endregion
        public RegionsCardImprovementToMarkdownAndDepartmentNumbers(IServiceProvider serviceProvider)
        {
            dbContext = serviceProvider.GetRequiredService<MemCheckDbContext>();
            logger = serviceProvider.GetRequiredService<ILogger<PaukerImportTest>>();
        }
        public void DescribeForOpportunityToCancel()
        {
            logger.LogInformation($"Will update region cards");
        }
        public async Task RunAsync()
        {
            var geoInfo = new GeoInfo();
            var user = await dbContext.Users.Where(u => u.UserName == "VoltanBot").SingleAsync();

            await UpdateCards_WhatIsThisRegionAsync(user.Id, geoInfo);
            await UpdateCards_WhereIsThisRegionAsync(user.Id, geoInfo);
            await UpdateCards_HowManyDepartmentsInThisRegionAsync(user.Id, geoInfo);
            await UpdateCards_WhatAreTheDepartmentsInThisRegionAsync(user.Id, geoInfo);

            await dbContext.SaveChangesAsync();

            logger.LogInformation($"Generation finished");
        }
    }
}
