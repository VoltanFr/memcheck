using MemCheck.Application.Cards;
using MemCheck.Basics;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemCheck.CommandLineDbClient.UsStates
{
    internal sealed class StatesCardCreator : ICmdLinePlugin
    {
        #region Fields
        private const string CardVersionDescription = "Created by VoltanBot StatesCardCreator";
        private readonly ILogger<StatesCardCreator> logger;
        private readonly MemCheckDbContext dbContext;
        #endregion
        #region classes State, InfoFileContents
        private sealed class State
        {
            #region Fields
            private Guid imageDbId = Guid.Empty;
            #endregion
            #region Private methods
            private static ImmutableArray<string> GetFields(string dataSetFileLine)
            {
                IEnumerable<string> fields = dataSetFileLine.Split(';');
                if (fields.Count() != 11)
                    throw new Exception($"Invalid line '{dataSetFileLine}'");
                return fields.ToImmutableArray();
            }
            #endregion
            public State(string dataSetFileLine)
            {
                //Nom de l'État;Article;Nom anglais;Surnom(s);Code;Rang;Surface;Population;Densité;Capitale;Ville
                try
                {
                    var fields = GetFields(dataSetFileLine);
                    FrenchName = fields[0];
                    Article = fields[1];
                    EnglishName = fields[2];
                    NickName = fields[3];
                    Code = fields[4];
                    Index = fields[5];
                    Surface = fields[6];
                    Population = fields[7];
                    Density = fields[8];
                    Capitale = fields[9];
                    MainCity = fields[10];

                    var additionalInfo = new StringBuilder();
                    var englishInfo = EnglishName == FrenchName ? "" : $" (en anglais _{EnglishName}_)";
                    if (Capitale == MainCity)
                        additionalInfo.AppendLine($"{ArticleWithFirstCharUpper}{FrenchName}{englishInfo} a pour capitale et ville la plus peuplée {Capitale}.");
                    else
                        additionalInfo.AppendLine($"{ArticleWithFirstCharUpper}{FrenchName}{englishInfo} a pour capitale {Capitale}, et sa ville la plus peuplée est {MainCity}.");
                    additionalInfo.AppendLine();
                    additionalInfo.AppendLine($"En 2019, cet État comptait {Population}habitants, pour une surface de {Surface} km², soit une densité de {Density} h/km² (la moyenne étant de 33 h/km²).");
                    additionalInfo.AppendLine();
                    additionalInfo.AppendLine($"Le surnom de l'État est _{NickName}_, son code est _{Code}_. C'est le {Index} des 50 États.");
                    AdditionalInfo = additionalInfo.ToString().Trim();
                }
                catch (Exception e)
                {
                    throw new Exception($"Failed to read region from '{dataSetFileLine}'", e);
                }
            }
            public string FrenchName { get; }
            public string Article { get; }
            public string ArticleWithFirstCharUpper
            {
                get
                {
                    if (Article.Length == 0)
                        return "";
                    return Article.First().ToString().ToUpper() + Article[1..];
                }
            }
            private string EnglishName { get; }
            private string NickName { get; }
            private string Code { get; }
            private string Index { get; }
            private string Surface { get; }
            private string Population { get; }
            private string Density { get; }
            public string Capitale { get; }
            public string MainCity { get; }
            public string AdditionalInfo { get; }
            public Guid GetImageDbId(MemCheckDbContext dbContext)
            {
                if (imageDbId != Guid.Empty)
                    return imageDbId;

                try
                {
                    return dbContext.Images.Where(img => img.Name == $"Situation {FrenchName}").Select(img => img.Id).Single();
                }
                catch
                {
                    throw new IOException($"Image not found: 'Situation {FrenchName}'");
                }
            }
        }
        private sealed class InfoFileContents
        {
            #region Fields
            #endregion
            public InfoFileContents()
            {
                var contents = File.ReadAllLines(@"C:\BackedUp\DocsBV\Synchronized\SkyDrive\Programmation\memcheck\MemCheck.CommandLineDbClient\UsStates\Resources\DataSet.txt");
                States = contents.Select(line => line.Trim()).Where(line => line.Length > 0).Select(line => new State(line));
            }
            public IEnumerable<State> States { get; }
        }
        #endregion
        #region Private methods
        private async Task CreateCard_WhatIsTheNameOfThisStateAsync(State state, MemCheckUser user, Guid statesWithNamesImageId, Guid frenchLanguageId, Guid tagId)
        {
            var frontSide = "Comment s'appelle cet État américain ?";
            var backSide = $"{state.ArticleWithFirstCharUpper}{state.FrenchName}";

            if (dbContext.Cards.Where(card => card.FrontSide == frontSide && card.BackSide == backSide).Any())
            {
                logger.LogInformation($"Card already exists for {state.FrenchName}: {frontSide}");
                return;
            }

            var frontSideImages = new[] { state.GetImageDbId(dbContext) };
            var backSideImages = Array.Empty<Guid>();

            var additionalInfo = state.AdditionalInfo;
            var additionalInfoImages = new[] { statesWithNamesImageId };

            var request = new CreateCard.Request(user.Id, frontSide, frontSideImages, backSide, backSideImages, additionalInfo, additionalInfoImages, frenchLanguageId, tagId.AsArray(), Array.Empty<Guid>(), CardVersionDescription);
            await new CreateCard(dbContext).RunAsync(request, new FakeStringLocalizer());
        }
        private async Task CreateCard_WhereIsThisStateAsync(State state, MemCheckUser user, Guid statesWithoutNamesImageId, Guid statesWithNamesImageId, Guid frenchLanguageId, Guid tagId)
        {
            var frontSide = $"Où est {state.Article}{state.FrenchName} sur cette carte ?";
            var backSide = "";

            if (dbContext.Cards.Where(card => card.FrontSide == frontSide && card.BackSide == backSide).Any())
            {
                logger.LogInformation($"Card already exists for {state.FrenchName}: {frontSide}");
                return;
            }

            var frontSideImages = new[] { statesWithoutNamesImageId };
            var backSideImages = new[] { state.GetImageDbId(dbContext) };

            var additionalInfo = state.AdditionalInfo;
            var additionalInfoImages = new[] { statesWithNamesImageId };

            var request = new CreateCard.Request(user.Id, frontSide, frontSideImages, backSide, backSideImages, additionalInfo, additionalInfoImages, frenchLanguageId, tagId.AsArray(), Array.Empty<Guid>(), CardVersionDescription);
            await new CreateCard(dbContext).RunAsync(request, new FakeStringLocalizer());
        }
        private async Task CreateCard_WhatIsTheCapitalOfAsync(State state, MemCheckUser user, Guid frenchLanguageId, Guid statesWithNamesImageId, Guid tagId)
        {
            var frontSide = $"Comment s'appelle la capitale de {state.Article}{state.FrenchName} ?";
            var backSide = state.Capitale;

            if (dbContext.Cards.Where(card => card.FrontSide == frontSide && card.BackSide == backSide).Any())
            {
                logger.LogInformation($"Card already exists for {state.FrenchName}: {frontSide}");
                return;
            }

            var frontSideImages = Array.Empty<Guid>();
            var backSideImages = Array.Empty<Guid>();

            var additionalInfo = state.AdditionalInfo;
            var additionalInfoImages = new[] { statesWithNamesImageId, state.GetImageDbId(dbContext) };

            var request = new CreateCard.Request(user.Id, frontSide, frontSideImages, backSide, backSideImages, additionalInfo, additionalInfoImages, frenchLanguageId, tagId.AsArray(), Array.Empty<Guid>(), CardVersionDescription);
            await new CreateCard(dbContext).RunAsync(request, new FakeStringLocalizer());
        }
        private async Task CreateCard_WhatIsTheStateOfThisCapitalAsync(State state, MemCheckUser user, Guid frenchLanguageId, Guid statesWithNamesImageId, Guid tagId)
        {
            var frontSide = $"De quel État américain {state.Capitale} est-elle la capitale ?";
            var backSide = $"{state.ArticleWithFirstCharUpper}{state.FrenchName}";

            if (dbContext.Cards.Where(card => card.FrontSide == frontSide && card.BackSide == backSide).Any())
            {
                logger.LogInformation($"Card already exists for {state.FrenchName}: {frontSide}");
                return;
            }

            var frontSideImages = Array.Empty<Guid>();
            var backSideImages = Array.Empty<Guid>();

            var additionalInfo = state.AdditionalInfo;
            var additionalInfoImages = new[] { statesWithNamesImageId, state.GetImageDbId(dbContext) };

            var request = new CreateCard.Request(user.Id, frontSide, frontSideImages, backSide, backSideImages, additionalInfo, additionalInfoImages, frenchLanguageId, tagId.AsArray(), Array.Empty<Guid>(), CardVersionDescription);
            await new CreateCard(dbContext).RunAsync(request, new FakeStringLocalizer());
        }
        private async Task CreateCard_WhatIsTheMainCityOfAsync(State state, MemCheckUser user, Guid frenchLanguageId, Guid statesWithNamesImageId, Guid tagId)
        {
            var frontSide = $"Comment s'appelle la ville la plus peuplée de {state.Article}{state.FrenchName} ?";
            var backSide = state.MainCity;

            if (dbContext.Cards.Where(card => card.FrontSide == frontSide && card.BackSide == backSide).Any())
            {
                logger.LogInformation($"Card already exists for {state.FrenchName}: {frontSide}");
                return;
            }

            var frontSideImages = Array.Empty<Guid>();
            var backSideImages = Array.Empty<Guid>();

            var additionalInfo = state.AdditionalInfo;
            var additionalInfoImages = new[] { state.GetImageDbId(dbContext), statesWithNamesImageId };

            var request = new CreateCard.Request(user.Id, frontSide, frontSideImages, backSide, backSideImages, additionalInfo, additionalInfoImages, frenchLanguageId, tagId.AsArray(), Array.Empty<Guid>(), CardVersionDescription);
            await new CreateCard(dbContext).RunAsync(request, new FakeStringLocalizer());
        }
        private async Task CreateCard_WhatIsTheStateOfThisCityAsync(State state, MemCheckUser user, Guid frenchLanguageId, Guid statesWithNamesImageId, Guid tagId)
        {
            var frontSide = $"Dans quel État américain la ville de {state.MainCity} se trouve-t-elle ?";
            var backSide = $"{state.ArticleWithFirstCharUpper}{state.FrenchName}";

            if (dbContext.Cards.Where(card => card.FrontSide == frontSide && card.BackSide == backSide).Any())
            {
                logger.LogInformation($"Card already exists for {state.FrenchName}: {frontSide}");
                return;
            }

            var frontSideImages = Array.Empty<Guid>();
            var backSideImages = Array.Empty<Guid>();

            var additionalInfo = state.AdditionalInfo;
            var additionalInfoImages = new[] { statesWithNamesImageId, state.GetImageDbId(dbContext) };

            var request = new CreateCard.Request(user.Id, frontSide, frontSideImages, backSide, backSideImages, additionalInfo, additionalInfoImages, frenchLanguageId, tagId.AsArray(), Array.Empty<Guid>(), CardVersionDescription);
            await new CreateCard(dbContext).RunAsync(request, new FakeStringLocalizer());
        }
        private async Task CreateCard_WhereIsThisCapitalAsync(State state, MemCheckUser user, Guid statesWithoutNamesImageId, Guid statesWitCapitalsImageId, Guid frenchLanguageId, Guid tagId)
        {
            var frontSide = $"Où est {state.Capitale} sur cette carte ?";
            var additionalInfo = state.AdditionalInfo;

            if (dbContext.Cards.Where(card => card.FrontSide == frontSide && card.AdditionalInfo == additionalInfo).Any())
            {
                logger.LogInformation($"Card already exists for {state.FrenchName}: {frontSide}");
                return;
            }

            var frontSideImages = statesWithoutNamesImageId.AsArray();
            var backSideImages = state.GetImageDbId(dbContext).AsArray();
            var additionalInfoImages = statesWitCapitalsImageId.AsArray();
            var backSide = "";

            var request = new CreateCard.Request(user.Id, frontSide, frontSideImages, backSide, backSideImages, additionalInfo, additionalInfoImages, frenchLanguageId, tagId.AsArray(), Array.Empty<Guid>(), CardVersionDescription);
            await new CreateCard(dbContext).RunAsync(request, new FakeStringLocalizer());
        }
        private async Task CreateCard_WhereIsMainCityAsync(State state, MemCheckUser user, Guid statesWithoutNamesImageId, Guid statesWithNamesImageId, Guid frenchLanguageId, Guid tagId)
        {
            var frontSide = $"Où est {state.MainCity} sur cette carte ?";
            var additionalInfo = state.AdditionalInfo;

            if (dbContext.Cards.Where(card => card.FrontSide == frontSide && card.AdditionalInfo == additionalInfo).Any())
            {
                logger.LogInformation($"Card already exists for {state.FrenchName}: {frontSide}");
                return;
            }

            var frontSideImages = statesWithoutNamesImageId.AsArray();
            var backSideImages = state.GetImageDbId(dbContext).AsArray();
            var additionalInfoImages = statesWithNamesImageId.AsArray();
            var backSide = "";

            var request = new CreateCard.Request(user.Id, frontSide, frontSideImages, backSide, backSideImages, additionalInfo, additionalInfoImages, frenchLanguageId, tagId.AsArray(), Array.Empty<Guid>(), CardVersionDescription);
            await new CreateCard(dbContext).RunAsync(request, new FakeStringLocalizer());
        }
        private async Task CreateCard_WhatIsTheCapitalOfThisStateAsync(State state, MemCheckUser user, Guid statesWitCapitalsImageId, Guid frenchLanguageId, Guid tagId)
        {
            var frontSide = "Quelle est la capitale de cet État américain ?";
            var backSide = $"{state.Capitale}";
            var additionalInfo = state.AdditionalInfo;

            if (dbContext.Cards.Where(card => card.FrontSide == frontSide && card.BackSide == backSide && card.AdditionalInfo == additionalInfo).Any())
            {
                logger.LogInformation($"Card already exists for {state.FrenchName}: {frontSide}");
                return;
            }

            var frontSideImages = state.GetImageDbId(dbContext).AsArray();
            var backSideImages = Array.Empty<Guid>();

            var additionalInfoImages = statesWitCapitalsImageId.AsArray();

            var request = new CreateCard.Request(user.Id, frontSide, frontSideImages, backSide, backSideImages, additionalInfo, additionalInfoImages, frenchLanguageId, tagId.AsArray(), Array.Empty<Guid>(), CardVersionDescription);
            await new CreateCard(dbContext).RunAsync(request, new FakeStringLocalizer());
        }
        private async Task CreateCard_WhatIsTheMainCityOfThisStateAsync(State state, MemCheckUser user, Guid statesWitCapitalsImageId, Guid frenchLanguageId, Guid tagId)
        {
            var frontSide = "Quelle est la ville la plus peuplée de cet État américain ?";
            var backSide = $"{state.MainCity}";
            var additionalInfo = state.AdditionalInfo;

            if (dbContext.Cards.Where(card => card.FrontSide == frontSide && card.BackSide == backSide && card.AdditionalInfo == additionalInfo).Any())
            {
                logger.LogInformation($"Card already exists for {state.FrenchName}: {frontSide}");
                return;
            }

            var frontSideImages = state.GetImageDbId(dbContext).AsArray();
            var backSideImages = Array.Empty<Guid>();

            var additionalInfoImages = statesWitCapitalsImageId.AsArray();

            var request = new CreateCard.Request(user.Id, frontSide, frontSideImages, backSide, backSideImages, additionalInfo, additionalInfoImages, frenchLanguageId, tagId.AsArray(), Array.Empty<Guid>(), CardVersionDescription);
            await new CreateCard(dbContext).RunAsync(request, new FakeStringLocalizer());
        }
        #endregion
        public StatesCardCreator(IServiceProvider serviceProvider)
        {
            dbContext = serviceProvider.GetRequiredService<MemCheckDbContext>();
            logger = serviceProvider.GetRequiredService<ILogger<StatesCardCreator>>();
        }
        public void DescribeForOpportunityToCancel()
        {
            logger.LogInformation($"Will create US states cards");
        }
        async public Task RunAsync()
        {
            var infoFileContents = new InfoFileContents();
            logger.LogInformation($"Loaded {infoFileContents.States.Count()} states");

            var user = dbContext.Users.Where(u => u.UserName == "VoltanBot").Single();//
            var frenchLanguageId = dbContext.CardLanguages.Where(lang => lang.Name == "Français").Select(lang => lang.Id).Single();
            var tagId = dbContext.Tags.Where(tag => tag.Name == "États américains").Select(tag => tag.Id).Single();
            var statesWithNamesImageId = dbContext.Images.Where(img => img.Name == "Carte.Amérique.USA.États").Select(img => img.Id).Single();
            var statesWithoutNamesImageId = dbContext.Images.Where(img => img.Name == "États américains sans noms").Select(img => img.Id).Single();
            var statesWitCapitalsImageId = dbContext.Images.Where(img => img.Name == "États américains avec capitales").Select(img => img.Id).Single();

            foreach (var state in infoFileContents.States)
            {
                logger.LogInformation($"************************ Working on state '{state.FrenchName}'");

                //await CreateCard_WhatIsTheNameOfThisStateAsync(state, user, statesWithNamesImageId, frenchLanguageId, tagId);
                //await CreateCard_WhereIsThisStateAsync(state, user, statesWithoutNamesImageId, statesWithNamesImageId, frenchLanguageId, tagId);
                //await CreateCard_WhatIsTheCapitalOfAsync(state, user, frenchLanguageId, statesWithNamesImageId, tagId);
                //await CreateCard_WhatIsTheStateOfThisCapitalAsync(state, user, frenchLanguageId, statesWithNamesImageId, tagId);
                //await CreateCard_WhatIsTheMainCityOfAsync(state, user, frenchLanguageId, statesWithNamesImageId, tagId);
                //await CreateCard_WhatIsTheStateOfThisCityAsync(state, user, frenchLanguageId, statesWithNamesImageId, tagId);
                await CreateCard_WhereIsThisCapitalAsync(state, user, statesWithoutNamesImageId, statesWitCapitalsImageId, frenchLanguageId, tagId);
                await CreateCard_WhereIsMainCityAsync(state, user, statesWithoutNamesImageId, statesWithNamesImageId, frenchLanguageId, tagId);
                await CreateCard_WhatIsTheCapitalOfThisStateAsync(state, user, statesWitCapitalsImageId, frenchLanguageId, tagId);
                await CreateCard_WhatIsTheMainCityOfThisStateAsync(state, user, statesWitCapitalsImageId, frenchLanguageId, tagId);
            }

            logger.LogInformation($"Generation finished");
        }
    }
}
