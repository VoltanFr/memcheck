using MemCheck.Application.Cards;
using MemCheck.Basics;
using MemCheck.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemCheck.CommandLineDbClient.Pauker
{
    internal sealed class BeaufortCardCreator : ICmdLinePlugin
    {
        #region Fields
        private const string CardVersionDescription = "Created by VoltanBot BeaufortCardCreator";
        private readonly ILogger<PaukerImportTest> logger;
        private readonly MemCheckDbContext dbContext;
        private readonly ImmutableArray<BeaufortForce> forceFromNumber;  //13 entries, index = force number
        private readonly ImmutableArray<BeaufortForce> forceFromWindSpeed;  //71 entries, index = wind speed
        private readonly string additionalInfo;
        #endregion
        #region BeaufortForce type
        private sealed record BeaufortForce(int Number, string Name, int MinWind, int MaxWind);
        private static ImmutableArray<BeaufortForce> GetForcesFromNumber()
        {
            var result = new List<BeaufortForce>
            {
                new BeaufortForce(0, "Calme", 0, 0),
                new BeaufortForce(1, "Très légère brise", 1, 3),
                new BeaufortForce(2, "Légère brise", 4, 6),
                new BeaufortForce(3, "Petite brise", 7, 10),
                new BeaufortForce(4, "Jolie brise", 11, 16),
                new BeaufortForce(5, "Bonne brise", 17, 21),
                new BeaufortForce(6, "Vent frais", 22, 27),
                new BeaufortForce(7, "Grand frais", 28, 33),
                new BeaufortForce(8, "Coup de vent", 34, 40),
                new BeaufortForce(9, "Fort coup de vent", 41, 47),
                new BeaufortForce(10, "Tempête", 48, 55),
                new BeaufortForce(11, "Violente tempête", 56, 63),
                new BeaufortForce(12, "Ouragan", 64, 70)
            };
            return result.ToImmutableArray();
        }
        private static ImmutableArray<BeaufortForce> GetForcesFromWindSpeed(ImmutableArray<BeaufortForce> forceFromNumber)
        {
            var result = new List<BeaufortForce>();
            foreach (var f in forceFromNumber)
                for (var speed = f.MinWind; speed <= f.MaxWind; speed++)
                    result.Add(f);
            return result.ToImmutableArray();
        }
        #endregion
        #region Private methods
        private static string GetAdditionalInfo()
        {
            var result = new StringBuilder();
            result.AppendLine("L'échelle de Beaufort est une échelle de mesure de la vitesse du vent utilisée dans le domaine maritime, graduée en 13 forces numérotées de 0 à 12 (chacune correspondant à un intervalle de vitesses de vent).");
            result.AppendLine("Dans ce tableau les vitesses de vent sont en nœuds, et les hauteurs de vagues sont en mètres.");
            result.AppendLine("| N° | Nom | Vent min | Vent max | Vagues | État de la mer |");
            result.AppendLine("|:--:|:--|:--:|:--:|:--:|:--|");
            result.AppendLine("| 0 | Calme | 0 | 0 | Quasi nulle | Comme un miroir, lisse |");
            result.AppendLine("| 1 | Très légère brise | 1 | 3 | 0 à 0,2 m | Quelques rides sans écume |");
            result.AppendLine("| 2 | Légère brise | 4 | 6 | 0,2 à 0,5 m | Vaguelettes ne déferlant pas |");
            result.AppendLine("| 3 | Petite brise | 7 | 10 | 0,5 à 1 m | Les crêtes commencent à déferler. Moutons épars |");
            result.AppendLine("| 4 | Jolie brise | 11 | 16 | 1 à 2 m | Nombreux moutons |");
            result.AppendLine("| 5 | Bonne brise | 17 | 21 | 2 à 3 m | Moutons, éventuellement embruns |");
            result.AppendLine("| 6 | Vent frais | 22 | 27 | 3 à 4 m | Crêtes d'écume blanches, lames, embruns |");
            result.AppendLine("| 7 | Grand frais | 28 | 33 | 4 à 5,5 m | Trainées d'écume, lames déferlantes |");
            result.AppendLine("| 8 | Coup de vent | 34 | 40 | 5,5 à 7,5 m | Tourbillons d'écume aux crêtes, trainées d'écume |");
            result.AppendLine("| 9 | Fort coup de vent | 41 | 47 | 7 à 10 m | Lames déferlantes grosses à énormes, visibilité réduite par les embruns |");
            result.AppendLine("| 10 | Tempête | 48 | 55 | 9 à 12,5 m | Très grosses lames, longues crêtes soufflées dans le vent.La surface semble blanche. Déferlement intense et brutal |");
            result.AppendLine("| 11 | Violente tempête | 56 | 63 | 11,5 à 16 m | Lames exceptionnellement hautes. Mer recouverte d'écume blanche et de mousse. Visibilité réduite |");
            result.AppendLine("| 12 | Ouragan | 64 | | 14 m et plus | L'air est plein d'écume et d'embruns. Mer entièrement blanche. Visibilité fortement réduite |");
            result.AppendLine("");
            result.AppendLine("Il s'agit de vitesses moyennes sur dix minutes (l'expression _un vent de 4 Beaufort avec des rafales à 6_ est incorrecte au sens formel, bien que très fréquente, utilisée par les CROSS par exemple).");
            result.AppendLine("L'échelle décrit aussi les effets du vent sur la surface, ce qui permet d'estimer rapidement la vitesse par observation de la mer.");
            result.AppendLine("Le symbole de l'échelle de Beaufort est _Bf_.");
            result.AppendLine("");
            result.AppendLine("En France, à partir de force 7, les CROSS émettent régulièrement des BMS (Bulletins Météorologiques Spéciaux) par VHF.");
            result.AppendLine("");
            result.AppendLine("L'échelle fut inventée en 1805 par le britannique Francis Beaufort.");
            return result.ToString();
        }
        private async Task GenerateMinSpeedForNameAsync(BeaufortForce f, Guid userId, Guid frenchLanguageId, Guid tagId)
        {
            var frontSide = $"Quelle est la plus faible vitesse de vent par force _{f.Name}_ ?{Environment.NewLine}(En nœuds)";
            if (dbContext.Cards.Where(card => card.VersionCreator.Id == userId && card.FrontSide == frontSide).Any())
            {
                logger.LogInformation($"Card already exists for {f.Name}: {frontSide}");
                return;
            }
            var backSide = $"{f.MinWind} nœuds";
            var request = new CreateCard.Request(userId, frontSide, Array.Empty<Guid>(), backSide, Array.Empty<Guid>(), additionalInfo, Array.Empty<Guid>(), frenchLanguageId, tagId.AsArray(), Array.Empty<Guid>(), CardVersionDescription);
            await new CreateCard(dbContext).RunAsync(request, new FakeStringLocalizer());
        }
        private async Task GenerateMaxSpeedForNameAsync(BeaufortForce f, Guid userId, Guid frenchLanguageId, Guid tagId)
        {
            var frontSide = $"Quelle est la plus grande vitesse de vent par force _{f.Name}_ ?{Environment.NewLine}(En nœuds)";
            if (dbContext.Cards.Where(card => card.VersionCreator.Id == userId && card.FrontSide == frontSide).Any())
            {
                logger.LogInformation($"Card already exists for {f.Name}: {frontSide}");
                return;
            }
            var backSide = $"{f.MaxWind} nœuds";
            var request = new CreateCard.Request(userId, frontSide, Array.Empty<Guid>(), backSide, Array.Empty<Guid>(), additionalInfo, Array.Empty<Guid>(), frenchLanguageId, tagId.AsArray(), Array.Empty<Guid>(), CardVersionDescription);
            await new CreateCard(dbContext).RunAsync(request, new FakeStringLocalizer());
        }
        private async Task GenerateMinSpeedForNumberAsync(BeaufortForce f, Guid userId, Guid frenchLanguageId, Guid tagId)
        {
            var frontSide = $"Quelle est la plus faible vitesse de vent par force n°{f.Number} ?{Environment.NewLine}(En nœuds)";
            if (dbContext.Cards.Where(card => card.VersionCreator.Id == userId && card.FrontSide == frontSide).Any())
            {
                logger.LogInformation($"Card already exists for {f.Name}: {frontSide}");
                return;
            }
            var backSide = $"{f.MinWind} nœuds";
            var request = new CreateCard.Request(userId, frontSide, Array.Empty<Guid>(), backSide, Array.Empty<Guid>(), additionalInfo, Array.Empty<Guid>(), frenchLanguageId, tagId.AsArray(), Array.Empty<Guid>(), CardVersionDescription);
            await new CreateCard(dbContext).RunAsync(request, new FakeStringLocalizer());
        }
        private async Task GenerateMaxSpeedForNumberAsync(BeaufortForce f, Guid userId, Guid frenchLanguageId, Guid tagId)
        {
            var frontSide = $"Quelle est la plus grande vitesse de vent par force n°{f.Number} ?{Environment.NewLine}(En nœuds)";
            if (dbContext.Cards.Where(card => card.VersionCreator.Id == userId && card.FrontSide == frontSide).Any())
            {
                logger.LogInformation($"Card already exists for {f.Name}: {frontSide}");
                return;
            }
            var backSide = $"{f.MaxWind} nœuds";
            var request = new CreateCard.Request(userId, frontSide, Array.Empty<Guid>(), backSide, Array.Empty<Guid>(), additionalInfo, Array.Empty<Guid>(), frenchLanguageId, tagId.AsArray(), Array.Empty<Guid>(), CardVersionDescription);
            await new CreateCard(dbContext).RunAsync(request, new FakeStringLocalizer());
        }
        private async Task GenerateSpeedIntervalForNumberAsync(BeaufortForce f, Guid userId, Guid frenchLanguageId, Guid tagId)
        {
            var frontSide = $"Quel est l'intervalle de vitesse de vent par force n°{f.Number} ?{Environment.NewLine}(En nœuds)";
            if (dbContext.Cards.Where(card => card.VersionCreator.Id == userId && card.FrontSide == frontSide).Any())
            {
                logger.LogInformation($"Card already exists for {f.Name}: {frontSide}");
                return;
            }
            var backSide = $"{f.MinWind} à {f.MaxWind} nœuds";
            var request = new CreateCard.Request(userId, frontSide, Array.Empty<Guid>(), backSide, Array.Empty<Guid>(), additionalInfo, Array.Empty<Guid>(), frenchLanguageId, tagId.AsArray(), Array.Empty<Guid>(), CardVersionDescription);
            await new CreateCard(dbContext).RunAsync(request, new FakeStringLocalizer());
        }
        private async Task GenerateSpeedIntervalForNameAsync(BeaufortForce f, Guid userId, Guid frenchLanguageId, Guid tagId)
        {
            var frontSide = $"Quel est l'intervalle de vitesse de vent par force _{f.Name}_ ?{Environment.NewLine}(En nœuds)";
            if (dbContext.Cards.Where(card => card.VersionCreator.Id == userId && card.FrontSide == frontSide).Any())
            {
                logger.LogInformation($"Card already exists for {f.Name}: {frontSide}");
                return;
            }
            var backSide = $"{f.MinWind} à {f.MaxWind} nœuds";
            var request = new CreateCard.Request(userId, frontSide, Array.Empty<Guid>(), backSide, Array.Empty<Guid>(), additionalInfo, Array.Empty<Guid>(), frenchLanguageId, tagId.AsArray(), Array.Empty<Guid>(), CardVersionDescription);
            await new CreateCard(dbContext).RunAsync(request, new FakeStringLocalizer());
        }
        private async Task GenerateSpeedMiddleForNumberAsync(BeaufortForce f, Guid userId, Guid frenchLanguageId, Guid tagId)
        {
            var frontSide = $"Quelle vitesse de vent est au milieu de l'intervalle par force n°{f.Number} ?{Environment.NewLine}(En nœuds)";
            if (dbContext.Cards.Where(card => card.VersionCreator.Id == userId && card.FrontSide == frontSide).Any())
            {
                logger.LogInformation($"Card already exists for {f.Name}: {frontSide}");
                return;
            }
            var backSide = $"{(f.MinWind + f.MaxWind) / 2} nœuds";
            CreateCard.Request? request = new CreateCard.Request(userId, frontSide, Array.Empty<Guid>(), backSide, Array.Empty<Guid>(), additionalInfo, Array.Empty<Guid>(), frenchLanguageId, tagId.AsArray(), Array.Empty<Guid>(), CardVersionDescription);
            await new CreateCard(dbContext).RunAsync(request, new FakeStringLocalizer());
        }
        private async Task GenerateSpeedMiddleForNameAsync(BeaufortForce f, Guid userId, Guid frenchLanguageId, Guid tagId)
        {
            var frontSide = $"Quelle vitesse de vent est au milieu de l'intervalle par force _{f.Name}_ ?{Environment.NewLine}(En nœuds)";
            if (dbContext.Cards.Where(card => card.VersionCreator.Id == userId && card.FrontSide == frontSide).Any())
            {
                logger.LogInformation($"Card already exists for {f.Name}: {frontSide}");
                return;
            }
            var backSide = $"{(f.MinWind + f.MaxWind) / 2} nœuds";
            var request = new CreateCard.Request(userId, frontSide, Array.Empty<Guid>(), backSide, Array.Empty<Guid>(), additionalInfo, Array.Empty<Guid>(), frenchLanguageId, tagId.AsArray(), Array.Empty<Guid>(), CardVersionDescription);
            await new CreateCard(dbContext).RunAsync(request, new FakeStringLocalizer());
        }
        private async Task GenerateNameFromSpeedAsync(int windSpeed, BeaufortForce f, Guid userId, Guid frenchLanguageId, Guid tagId)
        {
            var frontSide = $"Comment s'appelle la force Beaufort quand le vent va à {windSpeed} nœuds ?";
            if (dbContext.Cards.Where(card => card.VersionCreator.Id == userId && card.FrontSide == frontSide).Any())
            {
                logger.LogInformation($"Card already exists for {f.Name}: {frontSide}");
                return;
            }
            CreateCard.Request? request = new CreateCard.Request(userId, frontSide, Array.Empty<Guid>(), $"_{f.Name}_", Array.Empty<Guid>(), additionalInfo, Array.Empty<Guid>(), frenchLanguageId, tagId.AsArray(), Array.Empty<Guid>(), CardVersionDescription);
            await new CreateCard(dbContext).RunAsync(request, new FakeStringLocalizer());
        }
        private async Task GenerateNumberFromSpeedAsync(int windSpeed, BeaufortForce f, Guid userId, Guid frenchLanguageId, Guid tagId)
        {
            var frontSide = $"Quel est le numéro de force Beaufort quand le vent va à {windSpeed} nœuds ?";
            if (dbContext.Cards.Where(card => card.VersionCreator.Id == userId && card.FrontSide == frontSide).Any())
            {
                logger.LogInformation($"Card already exists for {f.Name}: {frontSide}");
                return;
            }
            var request = new CreateCard.Request(userId, frontSide, Array.Empty<Guid>(), $"{f.Number}", Array.Empty<Guid>(), additionalInfo, Array.Empty<Guid>(), frenchLanguageId, tagId.AsArray(), Array.Empty<Guid>(), CardVersionDescription);
            await new CreateCard(dbContext).RunAsync(request, new FakeStringLocalizer());
        }
        private async Task GenerateNameFromNumberAsync(BeaufortForce f, Guid userId, Guid frenchLanguageId, Guid tagId)
        {
            var frontSide = $"Comment s'appelle la force Beaufort n°{f.Number} ?";
            if (dbContext.Cards.Where(card => card.VersionCreator.Id == userId && card.FrontSide == frontSide).Any())
            {
                logger.LogInformation($"Card already exists for {f.Name}: {frontSide}");
                return;
            }
            var request = new CreateCard.Request(userId, frontSide, Array.Empty<Guid>(), $"_{f.Name}_", Array.Empty<Guid>(), additionalInfo, Array.Empty<Guid>(), frenchLanguageId, tagId.AsArray(), Array.Empty<Guid>(), CardVersionDescription);
            await new CreateCard(dbContext).RunAsync(request, new FakeStringLocalizer());
        }
        private async Task GenerateNumberFromNameAsync(BeaufortForce f, Guid userId, Guid frenchLanguageId, Guid tagId)
        {
            var frontSide = $"Quel est le numéro de la force Beaufort _{f.Name}_ ?";
            if (dbContext.Cards.Where(card => card.VersionCreator.Id == userId && card.FrontSide == frontSide).Any())
            {
                logger.LogInformation($"Card already exists for {f.Name}: {frontSide}");
                return;
            }
            var request = new CreateCard.Request(userId, frontSide, Array.Empty<Guid>(), $"_{f.Number}_", Array.Empty<Guid>(), additionalInfo, Array.Empty<Guid>(), frenchLanguageId, tagId.AsArray(), Array.Empty<Guid>(), CardVersionDescription);
            await new CreateCard(dbContext).RunAsync(request, new FakeStringLocalizer());
        }
        private async Task GenerateNameOfForceAboveNameAsync(BeaufortForce f, Guid userId, Guid frenchLanguageId, Guid tagId)
        {
            var frontSide = $"Comment s'appelle la force juste au-dessus de _{f.Name}_ ?";
            if (dbContext.Cards.Where(card => card.VersionCreator.Id == userId && card.FrontSide == frontSide).Any())
            {
                logger.LogInformation($"Card already exists for {f.Name}: {frontSide}");
                return;
            }
            if (f.Number + 1 >= forceFromNumber.Length)
                return;
            var backSide = $"_{forceFromNumber[f.Number + 1].Name}_";
            var request = new CreateCard.Request(userId, frontSide, Array.Empty<Guid>(), backSide, Array.Empty<Guid>(), additionalInfo, Array.Empty<Guid>(), frenchLanguageId, tagId.AsArray(), Array.Empty<Guid>(), CardVersionDescription);
            await new CreateCard(dbContext).RunAsync(request, new FakeStringLocalizer());
        }
        private async Task GenerateNameOfForceBelowNameAsync(BeaufortForce f, Guid userId, Guid frenchLanguageId, Guid tagId)
        {
            var frontSide = $"Comment s'appelle la force juste en-dessous de _{f.Name}_ ?";
            if (dbContext.Cards.Where(card => card.VersionCreator.Id == userId && card.FrontSide == frontSide).Any())
            {
                logger.LogInformation($"Card already exists for {f.Name}: {frontSide}");
                return;
            }
            if (f.Number - 1 < 0)
                return;
            var backSide = $"_{forceFromNumber[f.Number - 1].Name}_";
            var request = new CreateCard.Request(userId, frontSide, Array.Empty<Guid>(), backSide, Array.Empty<Guid>(), additionalInfo, Array.Empty<Guid>(), frenchLanguageId, tagId.AsArray(), Array.Empty<Guid>(), CardVersionDescription);
            await new CreateCard(dbContext).RunAsync(request, new FakeStringLocalizer());
        }
        private async Task GenerateNumberOfForceAboveNameAsync(BeaufortForce f, Guid userId, Guid frenchLanguageId, Guid tagId)
        {
            var frontSide = $"Quel est le numéro de la force juste au-dessus de _{f.Name}_ ?";
            if (dbContext.Cards.Where(card => card.VersionCreator.Id == userId && card.FrontSide == frontSide).Any())
            {
                logger.LogInformation($"Card already exists for {f.Name}: {frontSide}");
                return;
            }
            if (f.Number + 1 >= forceFromNumber.Length)
                return;
            var backSide = $"{forceFromNumber[f.Number + 1].Number}";
            var request = new CreateCard.Request(userId, frontSide, Array.Empty<Guid>(), backSide, Array.Empty<Guid>(), additionalInfo, Array.Empty<Guid>(), frenchLanguageId, tagId.AsArray(), Array.Empty<Guid>(), CardVersionDescription);
            await new CreateCard(dbContext).RunAsync(request, new FakeStringLocalizer());
        }
        private async Task GenerateNumberOfForceBelowNameAsync(BeaufortForce f, Guid userId, Guid frenchLanguageId, Guid tagId)
        {
            var frontSide = $"Quel est le numéro de la force juste en-dessous de _{f.Name}_ ?";
            if (dbContext.Cards.Where(card => card.VersionCreator.Id == userId && card.FrontSide == frontSide).Any())
            {
                logger.LogInformation($"Card already exists for {f.Name}: {frontSide}");
                return;
            }
            if (f.Number - 1 < 0)
                return;
            var backSide = $"{forceFromNumber[f.Number - 1].Number}";
            var request = new CreateCard.Request(userId, frontSide, Array.Empty<Guid>(), backSide, Array.Empty<Guid>(), additionalInfo, Array.Empty<Guid>(), frenchLanguageId, tagId.AsArray(), Array.Empty<Guid>(), CardVersionDescription);
            await new CreateCard(dbContext).RunAsync(request, new FakeStringLocalizer());
        }
        private async Task GenerateNameOfForceAboveNumberAsync(BeaufortForce f, Guid userId, Guid frenchLanguageId, Guid tagId)
        {
            var frontSide = $"Comment s'appelle la force Beaufort juste au-dessus de force {f.Number} ?";
            if (dbContext.Cards.Where(card => card.VersionCreator.Id == userId && card.FrontSide == frontSide).Any())
            {
                logger.LogInformation($"Card already exists for {f.Name}: {frontSide}");
                return;
            }
            if (f.Number + 1 >= forceFromNumber.Length)
                return;
            var backSide = $"_{forceFromNumber[f.Number + 1].Name}_";
            var request = new CreateCard.Request(userId, frontSide, Array.Empty<Guid>(), backSide, Array.Empty<Guid>(), additionalInfo, Array.Empty<Guid>(), frenchLanguageId, tagId.AsArray(), Array.Empty<Guid>(), CardVersionDescription);
            await new CreateCard(dbContext).RunAsync(request, new FakeStringLocalizer());
        }
        private async Task GenerateNameOfForceBelowNumberAsync(BeaufortForce f, Guid userId, Guid frenchLanguageId, Guid tagId)
        {
            var frontSide = $"Comment s'appelle la force Beaufort juste en-dessous de force {f.Number} ?";
            if (dbContext.Cards.Where(card => card.VersionCreator.Id == userId && card.FrontSide == frontSide).Any())
            {
                logger.LogInformation($"Card already exists for {f.Name}: {frontSide}");
                return;
            }
            if (f.Number - 1 < 0)
                return;
            var backSide = $"_{forceFromNumber[f.Number - 1].Name}_";
            var request = new CreateCard.Request(userId, frontSide, Array.Empty<Guid>(), backSide, Array.Empty<Guid>(), additionalInfo, Array.Empty<Guid>(), frenchLanguageId, tagId.AsArray(), Array.Empty<Guid>(), CardVersionDescription);
            await new CreateCard(dbContext).RunAsync(request, new FakeStringLocalizer());
        }
        #endregion
        public BeaufortCardCreator(IServiceProvider serviceProvider)
        {
            dbContext = serviceProvider.GetRequiredService<MemCheckDbContext>();
            logger = serviceProvider.GetRequiredService<ILogger<PaukerImportTest>>();
            forceFromNumber = GetForcesFromNumber();
            forceFromWindSpeed = GetForcesFromWindSpeed(forceFromNumber);
            additionalInfo = GetAdditionalInfo();
        }
        public void DescribeForOpportunityToCancel()
        {
            logger.LogInformation($"Will generate Beaufort scale cards");
        }
        async public Task RunAsync()
        {
            var user = dbContext.Users.Where(u => u.UserName == "VoltanBot").Single();
            var frenchLanguageId = dbContext.CardLanguages.Where(lang => lang.Name == "Français").Select(lang => lang.Id).Single();
            var tagId = dbContext.Tags.Where(tag => tag.Name == "Échelle de Beaufort").Select(tag => tag.Id).Single();

            for (var forceNumber = 0; forceNumber < 13; forceNumber++)
            {
                var force = forceFromNumber[forceNumber];
                logger.LogDebug($"Working on force '{force.Name}'");
                await GenerateMinSpeedForNameAsync(force, user.Id, frenchLanguageId, tagId);
                await GenerateMaxSpeedForNameAsync(force, user.Id, frenchLanguageId, tagId);
                await GenerateMinSpeedForNumberAsync(force, user.Id, frenchLanguageId, tagId);
                await GenerateMaxSpeedForNumberAsync(force, user.Id, frenchLanguageId, tagId);
                await GenerateSpeedIntervalForNumberAsync(force, user.Id, frenchLanguageId, tagId);
                await GenerateSpeedIntervalForNameAsync(force, user.Id, frenchLanguageId, tagId);
                await GenerateSpeedMiddleForNumberAsync(force, user.Id, frenchLanguageId, tagId);
                await GenerateSpeedMiddleForNameAsync(force, user.Id, frenchLanguageId, tagId);
                await GenerateNameFromNumberAsync(force, user.Id, frenchLanguageId, tagId);
                await GenerateNumberFromNameAsync(force, user.Id, frenchLanguageId, tagId);
                await GenerateNameOfForceAboveNameAsync(force, user.Id, frenchLanguageId, tagId);
                await GenerateNameOfForceBelowNameAsync(force, user.Id, frenchLanguageId, tagId);
                await GenerateNumberOfForceAboveNameAsync(force, user.Id, frenchLanguageId, tagId);
                await GenerateNumberOfForceBelowNameAsync(force, user.Id, frenchLanguageId, tagId);
                await GenerateNameOfForceAboveNumberAsync(force, user.Id, frenchLanguageId, tagId);
                await GenerateNameOfForceBelowNumberAsync(force, user.Id, frenchLanguageId, tagId);
            }

            for (var windSpeed = 0; windSpeed < forceFromWindSpeed.Length; windSpeed++)
            {
                await GenerateNameFromSpeedAsync(windSpeed, forceFromWindSpeed[windSpeed], user.Id, frenchLanguageId, tagId);
                await GenerateNumberFromSpeedAsync(windSpeed, forceFromWindSpeed[windSpeed], user.Id, frenchLanguageId, tagId);
            }

            logger.LogInformation($"Generation finished");
        }
    }
}
