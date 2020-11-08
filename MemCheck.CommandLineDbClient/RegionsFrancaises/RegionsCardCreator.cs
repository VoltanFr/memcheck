using MemCheck.Application;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace MemCheck.CommandLineDbClient.Pauker
{
    internal sealed class RegionsCardCreator : IMemCheckTest
    {
        #region Fields
        private const string CardVersionDescription = "Created by VoltanBot RegionsCardCreator";
        private readonly ILogger<PaukerImportTest> logger;
        private readonly MemCheckDbContext dbContext;
        private const string sourceDir = @"C:\BackedUp\DocsBV\Synchronized\SkyDrive\Programmation\CSharp\Vinny\MemCheck_v0.2\MemCheck.CommandLineDbClient\RegionsFrancaises";
        #endregion
        #region classes Region, InfoFileContents
        private sealed class Region
        {
            #region Fields
            private Guid imageDbId = Guid.Empty;
            #endregion
            #region Private methods
            private ImmutableArray<string> GetFields(string dataSetFileLine)
            {
                IEnumerable<string> fields = dataSetFileLine.Split(';');
                if (fields.Count() != 7)
                    throw new Exception($"Invalid line '{dataSetFileLine}'");
                return fields.Select(field => field.Trim()).ToImmutableArray();
            }
            private ImmutableArray<string> GetDepartments(string field, int expectedCount)
            {
                IEnumerable<string> fields = field.Split(',');
                if (fields.Count() != expectedCount)
                    throw new Exception($"Invalid department list '{field}'");
                return fields.Select(field => field.Trim()).ToImmutableArray();
            }
            private int GetDensity(string field)
            {
                var withoutBlanks = field.RemoveBlanks();
                int commaIndec = withoutBlanks.IndexOf(',');
                string s1 = withoutBlanks.Substring(0, commaIndec == -1 ? withoutBlanks.Length : commaIndec);
                return int.Parse(s1);
            }
            #endregion
            public Region(string dataSetFileLine)
            {
                try
                {
                    var fields = GetFields(dataSetFileLine);
                    Name = fields[0];
                    int departmentCount = int.Parse(fields[1]);
                    Population = int.Parse(fields[2].RemoveBlanks());
                    Density = GetDensity(fields[3]);
                    Departments = GetDepartments(fields[4], departmentCount);
                    ImageFileName = fields[5];
                    ImageSource = fields[6];
                }
                catch (Exception e)
                {
                    throw new Exception($"Failed to read region from '{dataSetFileLine}'", e);
                }
            }
            public string Name { get; }
            public int Population { get; }
            public int Density { get; }
            public ImmutableArray<string> Departments { get; }
            public string ImageFileName { get; }
            public string ImageSource { get; }
            public string ImageDbName => $"Carte.Europe.France.{Name}";
            public Guid GetImageDbId(MemCheckDbContext dbContext)
            {
                if (imageDbId != Guid.Empty)
                    return imageDbId;

                if (dbContext.Images.Where(img => img.Name == ImageDbName).Any())
                    imageDbId = dbContext.Images.Where(img => img.Name == ImageDbName).Select(img => img.Id).Single();

                return imageDbId;
            }
        }
        private sealed class InfoFileContents
        {
            #region Fields
            #endregion
            public InfoFileContents()
            {
                var contents = File.ReadAllLines(@"C:\BackedUp\DocsBV\Synchronized\SkyDrive\Programmation\CSharp\Vinny\MemCheck_v0.2\MemCheck.CommandLineDbClient\RegionsFrancaises\DataSet.csv");
                Regions = contents.Select(line => line.Trim()).Where(line => line.Length > 0).Select(line => new Region(line));
            }
            public IEnumerable<Region> Regions { get; }
        }
        private sealed class FakeStringLocalizer : IStringLocalizer
        {
            public LocalizedString this[string name] => new LocalizedString(name, "no translation");

            public LocalizedString this[string name, params object[] arguments] => this[name];

            public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            {
                return new LocalizedString[0];
            }

            public IStringLocalizer WithCulture(CultureInfo culture)
            {
                return this;
            }
        }
        #endregion
        #region Private methods
        private async Task InsertImageInDbAsync(Region region, MemCheckUser user)
        {
            if (region.GetImageDbId(dbContext) != Guid.Empty)
            {
                logger.LogDebug($"Image for region '{region.Name}' already in DB");
                return;
            }

            logger.LogDebug($"Inserting image in DB for region '{region.Name}'");

            using (var stream = File.OpenRead(Path.Combine(sourceDir, region.ImageFileName)))
            using (var reader = new BinaryReader(stream))
            {
                var blob = reader.ReadBytes((int)stream.Length);
                var request = new StoreImage.Request(user, region.ImageDbName, $"Région {region.Name} dans la carte de France", region.ImageSource, "image/svg+xml", blob);
                await new StoreImage(dbContext, new FakeStringLocalizer()).RunAsync(request);
            }
        }
        private async Task CreateCard_WhatIsThisRegionAsync(Region region, MemCheckUser user, Guid regionsAndDepartmentsWithNamesImageId, Guid frenchLanguageId, Guid tagId)
        {
            var frontSide = "Comment s'appelle cette région française ?";
            var backSide = region.Name;

            if (dbContext.Cards.Where(card => card.VersionCreator.Id == user.Id && card.FrontSide == frontSide && card.BackSide == backSide).Any())
            {
                logger.LogInformation($"Card already exists for {region.Name}: {frontSide}");
                return;
            }

            var frontSideImages = new[] { region.GetImageDbId(dbContext) };
            var backSideImages = new[] { regionsAndDepartmentsWithNamesImageId };
            var additionalInfo = $"Elle est constituée de ces {region.Departments.Count()} départements :{Environment.NewLine}{string.Join(Environment.NewLine, region.Departments)}{Environment.NewLine}{Environment.NewLine}En 2017, sa densité était de {region.Density} habitants par km² (la moyenne métropolitaine étant de 168 h/km²).";
            var request = new CreateCard.Request(user, frontSide, frontSideImages, backSide, backSideImages, additionalInfo, new Guid[0], frenchLanguageId, new[] { tagId }, new Guid[0], CardVersionDescription);
            await new CreateCard(dbContext).RunAsync(request, new FakeStringLocalizer());
        }
        private async Task CreateCard_WhereIsThisRegionAsync(Region region, MemCheckUser user, Guid regionsWithoutNamesImageId, Guid regionsAndDepartmentsWithNamesImageId, Guid frenchLanguageId, Guid tagId)
        {
            var frontSide = $"Où est la région {region.Name} ?";

            if (dbContext.Cards.Where(card => card.VersionCreator.Id == user.Id && card.FrontSide == frontSide).Any())
            {
                logger.LogInformation($"Card already exists for {region.Name}: {frontSide}");
                return;
            }

            var backSide = "";
            var frontSideImages = new[] { regionsWithoutNamesImageId };
            var backSideImages = new[] { region.GetImageDbId(dbContext) };
            var additionalInfo = $"Elle est constituée de ces {region.Departments.Count()} départements :{Environment.NewLine}{string.Join(Environment.NewLine, region.Departments)}{Environment.NewLine}{Environment.NewLine}En 2017, sa densité était de {region.Density} habitants par km² (la moyenne métropolitaine étant de 168 h/km²).";
            var additionalInfoImages = new[] { regionsAndDepartmentsWithNamesImageId };
            var request = new CreateCard.Request(user, frontSide, frontSideImages, backSide, backSideImages, additionalInfo, additionalInfoImages, frenchLanguageId, new[] { tagId }, new Guid[0], CardVersionDescription);
            await new CreateCard(dbContext).RunAsync(request, new FakeStringLocalizer());
        }
        private async Task CreateCard_HowManyDepartmentsInThisRegionAsync(Region region, MemCheckUser user, Guid regionsAndDepartmentsWithNamesImageId, Guid frenchLanguageId, Guid tagId)
        {
            var frontSide = $"Combien y a-t-il de départements dans la région {region.Name} ?";

            if (dbContext.Cards.Where(card => card.VersionCreator.Id == user.Id && card.FrontSide == frontSide).Any())
            {
                logger.LogInformation($"Card already exists for {region.Name}: {frontSide}");
                return;
            }

            var backSide = region.Departments.Count().ToString();
            var frontSideImages = new Guid[0];
            var backSideImages = new[] { regionsAndDepartmentsWithNamesImageId };
            var additionalInfo = string.Join(Environment.NewLine, region.Departments);
            var additionalInfoImages = new Guid[0];
            var request = new CreateCard.Request(user, frontSide, frontSideImages, backSide, backSideImages, additionalInfo, additionalInfoImages, frenchLanguageId, new[] { tagId }, new Guid[0], CardVersionDescription);
            await new CreateCard(dbContext).RunAsync(request, new FakeStringLocalizer());
        }
        private async Task CreateCard_WhatAreTheDepartmentsInThisRegionAsync(Region region, MemCheckUser user, Guid regionsAndDepartmentsWithNamesImageId, Guid frenchLanguageId, Guid tagId)
        {
            var frontSide = $"Quels départements y a-t-il dans la région {region.Name} ?";

            if (dbContext.Cards.Where(card => card.VersionCreator.Id == user.Id && card.FrontSide == frontSide).Any())
            {
                logger.LogInformation($"Card already exists for {region.Name}: {frontSide}");
                return;
            }

            var backSide = string.Join(Environment.NewLine, region.Departments);
            var frontSideImages = new Guid[0];
            var backSideImages = new[] { regionsAndDepartmentsWithNamesImageId };
            var additionalInfo = $"La région {region.Name} est constituée de {region.Departments.Count()} départements.{Environment.NewLine}En 2017, sa densité était de {region.Density} habitants par km² (la moyenne métropolitaine étant de 168 h/km²).";
            var additionalInfoImages = new Guid[0];
            var request = new CreateCard.Request(user, frontSide, frontSideImages, backSide, backSideImages, additionalInfo, additionalInfoImages, frenchLanguageId, new[] { tagId }, new Guid[0], CardVersionDescription);
            await new CreateCard(dbContext).RunAsync(request, new FakeStringLocalizer());
        }
        #endregion
        public RegionsCardCreator(IServiceProvider serviceProvider)
        {
            dbContext = serviceProvider.GetService<MemCheckDbContext>();
            logger = serviceProvider.GetService<ILogger<PaukerImportTest>>();
        }
        public void DescribeForOpportunityToCancel()
        {
            logger.LogInformation($"Will create region cards from path {sourceDir}");
        }
        async public Task RunAsync(MemCheckDbContext dbContext)
        {
            var infoFileContents = new InfoFileContents();
            logger.LogInformation($"Loaded {infoFileContents.Regions.Count()} regions");

            var user = dbContext.Users.Where(u => u.UserName == "VoltanBot").Single();
            var frenchLanguageId = dbContext.CardLanguages.Where(lang => lang.Name == "Français").Select(lang => lang.Id).Single();
            var tagId = dbContext.Tags.Where(tag => tag.Name == "Régions françaises").Select(tag => tag.Id).Single();
            var regionsAndDepartmentsWithNamesImageId = dbContext.Images.Where(img => img.Name == "Carte.Europe.France.Régions et Départements avec noms").Select(img => img.Id).Single();
            var regionsWithoutNamesImageId = dbContext.Images.Where(img => img.Name == "Carte.Europe.France.Régions sans noms").Select(img => img.Id).Single();

            foreach (var region in infoFileContents.Regions)
            {
                logger.LogDebug($"Working on region '{region.Name}'");

                await InsertImageInDbAsync(region, user);
                await CreateCard_WhatIsThisRegionAsync(region, user, regionsAndDepartmentsWithNamesImageId, frenchLanguageId, tagId);
                await CreateCard_WhereIsThisRegionAsync(region, user, regionsWithoutNamesImageId, regionsAndDepartmentsWithNamesImageId, frenchLanguageId, tagId);
                await CreateCard_HowManyDepartmentsInThisRegionAsync(region, user, regionsAndDepartmentsWithNamesImageId, frenchLanguageId, tagId);
                await CreateCard_WhatAreTheDepartmentsInThisRegionAsync(region, user, regionsAndDepartmentsWithNamesImageId, frenchLanguageId, tagId);
            }

            logger.LogInformation($"Generation finished");
        }
    }
}
