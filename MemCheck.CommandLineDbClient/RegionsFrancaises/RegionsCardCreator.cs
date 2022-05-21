using MemCheck.Application.Cards;
using MemCheck.Application.Images;
using MemCheck.Basics;
using MemCheck.CommandLineDbClient.Pauker;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.CommandLineDbClient.RegionsFrancaises;

internal sealed class RegionsCardCreator : ICmdLinePlugin
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
        private static ImmutableArray<string> GetFields(string dataSetFileLine)
        {
            var fields = dataSetFileLine.Split(';');
            return fields.Length == 7
                ? fields.Select(field => field.Trim()).ToImmutableArray()
                : throw new ArgumentException($"Invalid line '{dataSetFileLine}'");
        }
        private static ImmutableArray<string> GetDepartments(string field, int expectedCount)
        {
            IEnumerable<string> fields = field.Split(',');
            return fields.Count() == expectedCount
                ? fields.Select(field => field.Trim()).ToImmutableArray()
                : throw new ArgumentException($"Invalid department list '{field}'");
        }
        private static int GetDensity(string field)
        {
            var withoutBlanks = field.RemoveBlanks();
            var commaIndex = withoutBlanks.IndexOf(',');
            var s1 = withoutBlanks[..(commaIndex == -1 ? withoutBlanks.Length : commaIndex)];
            return int.Parse(s1);
        }
        #endregion
        public Region(string dataSetFileLine)
        {
            try
            {
                var fields = GetFields(dataSetFileLine);
                Name = fields[0];
                var departmentCount = int.Parse(fields[1]);
                Population = int.Parse(fields[2].RemoveBlanks());
                Density = GetDensity(fields[3]);
                Departments = GetDepartments(fields[4], departmentCount);
                ImageFileName = fields[5];
                ImageSource = fields[6];
            }
            catch (Exception e)
            {
                throw new IOException($"Failed to read region from '{dataSetFileLine}'", e);
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

        using var stream = File.OpenRead(Path.Combine(sourceDir, region.ImageFileName));
        using var reader = new BinaryReader(stream);
        var blob = reader.ReadBytes((int)stream.Length);
        var request = new StoreImage.Request(user.Id, region.ImageDbName, $"Région {region.Name} dans la carte de France", region.ImageSource, "image/svg+xml", blob);
        await new StoreImage(dbContext.AsCallContext()).RunAsync(request);
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
        var additionalInfo = $"Elle est constituée de ces {region.Departments.Length} départements :{Environment.NewLine}{string.Join(Environment.NewLine, region.Departments)}{Environment.NewLine}{Environment.NewLine}En 2017, sa densité était de {region.Density} habitants par km² (la moyenne métropolitaine étant de 168 h/km²).";
        var request = new CreateCard.Request(user.Id, frontSide, frontSideImages, backSide, backSideImages, additionalInfo, Array.Empty<Guid>(), "", frenchLanguageId, tagId.AsArray(), Array.Empty<Guid>(), CardVersionDescription);
        await new CreateCard(dbContext.AsCallContext()).RunAsync(request);
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
        var additionalInfo = $"Elle est constituée de ces {region.Departments.Length} départements :{Environment.NewLine}{string.Join(Environment.NewLine, region.Departments)}{Environment.NewLine}{Environment.NewLine}En 2017, sa densité était de {region.Density} habitants par km² (la moyenne métropolitaine étant de 168 h/km²).";
        var additionalInfoImages = new[] { regionsAndDepartmentsWithNamesImageId };
        var request = new CreateCard.Request(user.Id, frontSide, frontSideImages, backSide, backSideImages, additionalInfo, additionalInfoImages, "", frenchLanguageId, tagId.AsArray(), Array.Empty<Guid>(), CardVersionDescription);
        await new CreateCard(dbContext.AsCallContext()).RunAsync(request);
    }
    private async Task CreateCard_HowManyDepartmentsInThisRegionAsync(Region region, MemCheckUser user, Guid regionsAndDepartmentsWithNamesImageId, Guid frenchLanguageId, Guid tagId)
    {
        var frontSide = $"Combien y a-t-il de départements dans la région {region.Name} ?";

        if (dbContext.Cards.Where(card => card.VersionCreator.Id == user.Id && card.FrontSide == frontSide).Any())
        {
            logger.LogInformation($"Card already exists for {region.Name}: {frontSide}");
            return;
        }

        var backSide = region.Departments.Length.ToString();
        var frontSideImages = Array.Empty<Guid>();
        var backSideImages = new[] { regionsAndDepartmentsWithNamesImageId };
        var additionalInfo = string.Join(Environment.NewLine, region.Departments);
        var additionalInfoImages = Array.Empty<Guid>();
        var request = new CreateCard.Request(user.Id, frontSide, frontSideImages, backSide, backSideImages, additionalInfo, additionalInfoImages, "", frenchLanguageId, tagId.AsArray(), Array.Empty<Guid>(), CardVersionDescription);
        await new CreateCard(dbContext.AsCallContext()).RunAsync(request);
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
        var frontSideImages = Array.Empty<Guid>();
        var backSideImages = new[] { regionsAndDepartmentsWithNamesImageId };
        var additionalInfo = $"La région {region.Name} est constituée de {region.Departments.Length} départements.{Environment.NewLine}En 2017, sa densité était de {region.Density} habitants par km² (la moyenne métropolitaine étant de 168 h/km²).";
        var additionalInfoImages = Array.Empty<Guid>();
        var request = new CreateCard.Request(user.Id, frontSide, frontSideImages, backSide, backSideImages, additionalInfo, additionalInfoImages, "", frenchLanguageId, tagId.AsArray(), Array.Empty<Guid>(), CardVersionDescription);
        await new CreateCard(dbContext.AsCallContext()).RunAsync(request);
    }
    #endregion
    public RegionsCardCreator(IServiceProvider serviceProvider)
    {
        dbContext = serviceProvider.GetRequiredService<MemCheckDbContext>();
        logger = serviceProvider.GetRequiredService<ILogger<PaukerImportTest>>();
    }
    public void DescribeForOpportunityToCancel()
    {
        logger.LogInformation($"Will create region cards from path {sourceDir}");
    }
    public async Task RunAsync()
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
