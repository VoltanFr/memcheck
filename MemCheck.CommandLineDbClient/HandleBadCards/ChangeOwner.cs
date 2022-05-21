using MemCheck.CommandLineDbClient.Pauker;
using MemCheck.CommandLineDbClient.RegionsFrancaises;
using MemCheck.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.CommandLineDbClient.HandleBadCards;

internal sealed class ChangeOwner : ICmdLinePlugin
{
    #region Fields
    private readonly ILogger<PaukerImportTest> logger;
    private readonly MemCheckDbContext dbContext;
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
            var fields = field.Split(',');
            return fields.Length == expectedCount
                ? fields.Select(field => field.Trim()).ToImmutableArray()
                : throw new ArgumentException($"Invalid department list '{field}'");
        }
        private static int GetDensity(string field)
        {
            var withoutBlanks = field.RemoveBlanks();
            int commaIndec = withoutBlanks.IndexOf(',');
            string s1 = withoutBlanks[..(commaIndec == -1 ? withoutBlanks.Length : commaIndec)];
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
    public ChangeOwner(IServiceProvider serviceProvider)
    {
        dbContext = serviceProvider.GetRequiredService<MemCheckDbContext>();
        logger = serviceProvider.GetRequiredService<ILogger<PaukerImportTest>>();
    }
    public void DescribeForOpportunityToCancel()
    {
        logger.LogInformation("Will change owners of cards and images with null ones");
    }
    public async Task RunAsync()
    {
        var user = dbContext.Users.Where(user => user.UserName == "Toto1").Single();

        var images = dbContext.Images.Where(image => image.Owner == null);
        logger.LogInformation($"Found {images.Count()} images to modify");
        foreach (var image in images)
        {
            logger.LogDebug($"Changing image '{image.Name}'");
            image.Owner = user;
        }

        var imagePreviousVersions = dbContext.ImagePreviousVersions.Where(image => image.Owner == null);
        logger.LogInformation($"Found {imagePreviousVersions.Count()} imagePreviousVersions to modify");
        foreach (var imagePreviousVersion in imagePreviousVersions)
        {
            logger.LogDebug($"Changing imagePreviousVersion '{imagePreviousVersion.Name}'");
            imagePreviousVersion.Owner = user;
        }

        var cards = dbContext.Cards.Where(card => card.VersionCreator == null);
        logger.LogInformation($"Found {cards.Count()} cards to modify");
        foreach (var card in cards)
        {
            logger.LogDebug($"Changing card with front side {card.FrontSide}");
            card.VersionCreator = user;
        }

        await dbContext.SaveChangesAsync();
        logger.LogInformation($"Finished");
    }
}
