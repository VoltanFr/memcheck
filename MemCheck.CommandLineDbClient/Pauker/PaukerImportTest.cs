using MemCheck.Application.Cards;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace MemCheck.CommandLineDbClient.Pauker
{
    internal sealed class PaukerImportTest : ICmdLinePlugin
    {
        #region Fields
        private const string CardVersionDescription = "Created by VoltanBot PaukerImportTest";
        private readonly ILogger<PaukerImportTest> logger;
        private readonly MemCheckDbContext dbContext;
        private const string filePath = @"C:\BackedUp\DocsBV\Synchronized\SkyDrive\Pauker\Vince.pau.gz";
        #endregion
        #region Private methods
        private static string StringFromGZipFile(string file, Encoding encoding)
        {
            //StringBuilder result = new StringBuilder();
            using FileStream fs = new(file, FileMode.Open, FileAccess.Read);
            using GZipStream gz = new(fs, CompressionMode.Decompress);
            List<byte> allBytes = new();
            byte[] buffer = new byte[4096];
            int numRead;
            while ((numRead = gz.Read(buffer, 0, buffer.Length)) != 0)
                for (int i = 0; i < numRead; i++)
                    allBytes.Add(buffer[i]);
            return encoding.GetString(allBytes.ToArray());
        }
        private static DateTime PaukerExpiryDate(PaukerCard card, int stackIndex)
        {
            var nbDaysForExpiration = Math.Exp(stackIndex - 1);
            return card.LearnedDate().AddDays(nbDaysForExpiration);
        }
        private static int GetBestMemCheckHeap(PaukerCard card, int stackIndex)
        {
            var learnedDate = card.LearnedDate();
            var paukerExpiry = PaukerExpiryDate(card, stackIndex);
            var resultDistanceToPauker = int.MaxValue;
            var result = 0;

            for (int i = 1; i < 20; i++)
            {
                double nbDaysOfExpiry = Math.Pow(2, i);
                var memCheckExpiryInThisHeap = learnedDate.AddDays(nbDaysOfExpiry);

                var distanceToPauker = (int)(paukerExpiry - memCheckExpiryInThisHeap).TotalDays;
                var absDist = Math.Abs(distanceToPauker);

                if (absDist < resultDistanceToPauker)
                {
                    result = i;
                    resultDistanceToPauker = absDist;
                }
            }

            //if (resultDistanceToPauker != 72)
            //    logger.LogDebug($"resultDistanceToPauker: {resultDistanceToPauker} days");


            //logger.LogDebug($"Best heap for card is {result}, distance to Pauker is {resultDistanceToPauker} days");
            //logger.LogDebug($"Pauker expiry date: {paukerExpiry}");
            //DateTime memCheckExpiry = learnedDate.AddDays(Math.Pow(2, result));
            //logger.LogDebug($"MemCheck expiry date: {memCheckExpiry}");
            //return memCheckExpiry <= DateTime.Now;
            return result;
        }
        private static PaukerLesson GetPaukerLesson()
        {
            var paukerXml = StringFromGZipFile(filePath, Encoding.UTF8);
            var doc = new XmlDocument();
            doc.Load(new StringReader(paukerXml));
            var lesson = new PaukerLesson(doc, new FileInfo(filePath));
            lesson.RemoveDoublons();
            return lesson;
        }
        #endregion
        public PaukerImportTest(IServiceProvider serviceProvider)
        {
            dbContext = serviceProvider.GetRequiredService<MemCheckDbContext>();
            logger = serviceProvider.GetRequiredService<ILogger<PaukerImportTest>>();
        }
        public void DescribeForOpportunityToCancel()
        {
            logger.LogInformation($"Will play with {filePath}");
        }
        async public Task RunAsync()
        {
            var lesson = GetPaukerLesson();

            Console.WriteLine(string.Join(',', dbContext.Users));

            var user = dbContext.Users.Where(user => user.UserName == "Voltan").Single();
            var deck = dbContext.Decks.Where(deck => deck.Owner == user).First();
            //var tag = dbContext.Tags.Where(tag => tag.Name == "PaukerImport").Single();
            var cardLanguage = dbContext.CardLanguages.Where(cardLanguage => cardLanguage.Name == "Français").Single();

            logger.LogInformation($"Cards will be added to deck {deck.Description} of user {user.UserName}");// and tag {tag.Name} will be attached");

            const int paukerDisplayedStackIndex = 1;
            var stack = lesson.Stacks[paukerDisplayedStackIndex + 2];
            logger.LogDebug($"Working on stack '{stack.Name}'");
            logger.LogDebug($"Stack contains {stack.Cards.Count} cards");

            for (int cardIndex = 0; cardIndex < stack.Cards.Count; cardIndex++)
            {
                logger.LogInformation($"Working on card {cardIndex} of {stack.Cards.Count}");
                PaukerCard paukerCard = stack.Cards[cardIndex];
                if (dbContext.Cards.Where(card => card.FrontSide == paukerCard.Front.Text.Trim()).Any())
                    logger.LogInformation($"Card already exists in MemCheck with this front, skipping: {paukerCard.Front.Text}");
                else
                {
                    CreateCard.Request request = new(
                        user.Id,
                        paukerCard.Front.Text.Trim(),
                        Array.Empty<Guid>(),
                        paukerCard.Reverse.Text.Trim(),
                        Array.Empty<Guid>(),
                        "",
                        Array.Empty<Guid>(),
                        "",
                        cardLanguage.Id,
                        Array.Empty<Guid>(),
                        new[] { user.Id },
                        CardVersionDescription);

                    Card card = new()
                    {
                        FrontSide = request.FrontSide,
                        BackSide = request.BackSide,
                        AdditionalInfo = request.AdditionalInfo,
                        CardLanguage = cardLanguage,
                        VersionCreator = user,
                        InitialCreationUtcDate = DateTime.Now.ToUniversalTime(),
                        VersionUtcDate = DateTime.Now.ToUniversalTime()
                    };
                    await dbContext.Cards.AddAsync(card);

                    var usersWithView = new List<UserWithViewOnCard>();
                    var userWithView = new UserWithViewOnCard() { UserId = user.Id, User = user, CardId = card.Id, Card = card };
                    dbContext.UsersWithViewOnCards.Add(userWithView);
                    usersWithView.Add(userWithView);
                    card.UsersWithView = usersWithView;



                    //return new GetCardsOfUser.ViewModel(card.Id, card.FrontSide, card.BackSide, card.TagsInCards.Select(tagInCard => tagInCard.Tag.Name));





                    //var card = await creator.RunAsync(request, user);

                    var targetHeap = GetBestMemCheckHeap(paukerCard, paukerDisplayedStackIndex);

                    var cardInDeck = new CardInDeck()
                    {
                        CardId = card.Id,
                        DeckId = deck.Id,
                        CurrentHeap = targetHeap,
                        LastLearnUtcTime = paukerCard.LearnedDate().ToUniversalTime(),
                        AddToDeckUtcTime = DateTime.UtcNow,
                        NbTimesInNotLearnedHeap = 1,
                        BiggestHeapReached = targetHeap
                    };
                    await dbContext.CardsInDecks.AddAsync(cardInDeck);

                    //dbContext.SaveChanges();

                    //            var cardLoaded = dbContext.Cards.Where(card => card.Id == memCheckCard.Id).Include(card => card.UsersWithView).Single();
                    //            if (!cardLoaded.UsersWithView.Contains(user))
                    //                throw new ApplicationException();
                }

                //logger.LogDebug($"In stack {paukerDisplayedStackIndex}, {expiredCount} would be expired");
            }
            await dbContext.SaveChangesAsync();
        }
    }
}
