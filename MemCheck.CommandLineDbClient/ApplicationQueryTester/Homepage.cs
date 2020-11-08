using MemCheck.Application;
using MemCheck.Application.Heaping;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace MemCheck.CommandLineDbClient.ApplicationQueryTester
{
    internal sealed class Homepage : IMemCheckTest
    {
        #region Fields
        private readonly ILogger<GetCards> logger;
        private readonly MemCheckDbContext dbContext;
        private bool realCode = true;
        #endregion
        public Homepage(IServiceProvider serviceProvider)
        {
            dbContext = serviceProvider.GetService<MemCheckDbContext>();
            logger = serviceProvider.GetService<ILogger<GetCards>>();
        }
        async public Task RunAsync(MemCheckDbContext dbContext)
        {
            var user = dbContext.Users.Where(user => user.UserName == "Voltan").Single();

            if (realCode)
            {
                var chronos = new List<double>();

                for (int i = 0; i < 5; i++)
                {
                    var realCodeChrono = Stopwatch.StartNew();
                    var userDecks = new GetDecksWithLearnCounts(dbContext).Run(user.Id);
                    logger.LogInformation($"{userDecks.First().CardCount} cards");
                    logger.LogInformation($"{userDecks.First().Description}");
                    logger.LogInformation($"{userDecks.First().ExpiredCardCount} expired");
                    logger.LogInformation($"{userDecks.First().UnknownCardCount} unknown");
                    logger.LogInformation($"{userDecks.First().NextExpiryUTCDate} next expiry");
                    logger.LogInformation($"{userDecks.Count()} decks in {realCodeChrono.Elapsed}");
                    chronos.Add(realCodeChrono.Elapsed.TotalSeconds);
                }

                logger.LogInformation($"Average time: {chronos.Average()} seconds");
            }
            else
            {
                var chrono = Stopwatch.StartNew();
                //var logLines = await GetCardsToRepeatAsync(user.Id, deck.Id, new Guid[0], new Guid[0]);
                //chrono.Stop();
                //foreach (var logLine in logLines)
                //    logger.LogInformation(logLine);
                logger.LogInformation($"Ran in {chrono.Elapsed}");
            }

            await Task.CompletedTask;
        }
        public void DescribeForOpportunityToCancel()
        {
            logger.LogInformation($"Will load homepage data");
        }
        private async Task<(Dictionary<Guid, double> averageRatings, Dictionary<Guid, int> userRatings, Dictionary<Guid, int> countOfUserRatings)> GetRatingsAsync(Guid userId, HashSet<Guid> cardIds)
        {
            //var allUsersRatings = await dbContext.UserCardRatings
            //    .Where(rating => cardIds.Contains(rating.CardId))
            //    .Select(userRating => new { userRating.UserId, userRating.CardId, userRating.Rating })
            //    .ToListAsync();

            var averageRatings = new Dictionary<Guid, double>();
            //allUsersRatings 
            //.GroupBy(userRating => userRating.CardId)
            //.Select(cardRatings => new { cardId = cardRatings.Key, average = cardRatings.Select(cardRating => cardRating.Rating).Average() })
            //.ToDictionary(rating => rating.cardId, rating => rating.average);

            var countOfUserRatings = new Dictionary<Guid, int>();
            //allUsersRatings
            //.GroupBy(userRating => userRating.CardId)
            //.Select(group => new { cardId = group.Key, count = group.Count() })
            //.ToDictionary(rating => rating.cardId, rating => rating.count);

            var userRatings = new Dictionary<Guid, int>();
            //allUsersRatings
            //.Where(rating => rating.UserId == userId)
            //.ToDictionary(userRating => userRating.CardId, userRating => userRating.Rating);

            await Task.CompletedTask;

            return (averageRatings, userRatings, countOfUserRatings);
        }
        private async Task<IEnumerable<string>> GetCardsToRepeatAsync(Guid userId, Guid deckId, IEnumerable<Guid> excludedCardIds, IEnumerable<Guid> excludedTagIds)
        {
            var result = new List<string>();
            var heapingAlgorithmId = await dbContext.Decks.Where(deck => deck.Id == deckId).Select(deck => deck.HeapingAlgorithmId).SingleAsync();
            var heapingAlgorithm = HeapingAlgorithms.Instance.FromId(heapingAlgorithmId);

            var chrono = Stopwatch.StartNew();
            var heaps = await dbContext.CardsInDecks.Where(cardInDeck => cardInDeck.DeckId == deckId && cardInDeck.CurrentHeap != 0).Select(cardInDeck => cardInDeck.CurrentHeap).Distinct().ToListAsync();
            result.Add($"Got heaps in {chrono.Elapsed}");

            var resultCards = new List<Guid>();
            const int cardCount = 30;

            foreach (var heap in heaps.OrderByDescending(heap => heap))
                if (result.Count < cardCount)
                {
                    chrono = Stopwatch.StartNew();
                    var cardsOfHeap = dbContext.CardsInDecks.Where(cardInDeck => cardInDeck.DeckId == deckId && cardInDeck.CurrentHeap == heap && !excludedCardIds.Contains(cardInDeck.CardId));

                    var withoutExcludedCards = cardsOfHeap;
                    foreach (var tag in excludedTagIds)   //I tried to do better with an intersect between the two sets, but that failed
                        withoutExcludedCards = withoutExcludedCards.Where(cardInDeck => !cardInDeck.Card.TagsInCards.Where(tagInCard => tagInCard.TagId == tag).Any());

                    var ordered = withoutExcludedCards.OrderBy(cardInDeck => cardInDeck.LastLearnUtcTime);
                    var oldest = ordered.Take(cardCount);

                    var withDetails = oldest
                        .Include(cardInDeck => cardInDeck.Card)
                        .ThenInclude(card => card.VersionCreator)
                        .Include(cardInDeck => cardInDeck.Card.TagsInCards)
                        .ThenInclude(tagInCard => tagInCard.Tag)
                        .Include(cardInDeck => cardInDeck.Card.UsersWithView)
                        .ThenInclude(userWithViewOnCard => userWithViewOnCard.User)
                        .Include(cardInDeck => cardInDeck.Card.Images);
                    //.ThenInclude(imageInCard => imageInCard.Image.Id);
                    //.ThenInclude(image => image.Owner);

                    var withDetailsListed = await withDetails.ToListAsync();
                    var resultCardIds = withDetailsListed.Select(cardInDeck => cardInDeck.CardId).ToHashSet();
                    //var (averageRatings, userRatings, countOfUserRatings) = await GetRatingsAsync(userId, resultCardIds);

                    var emptyStringArray = new string[0];
                    var emptyResultImageModelArray = new ResultImageModel[0];


                    var thisHeapResult = withDetailsListed.Select(oldestCard => new ResultCard(oldestCard.CardId, oldestCard.CurrentHeap, oldestCard.LastLearnUtcTime, oldestCard.AddToDeckUtcTime,
                        oldestCard.BiggestHeapReached, oldestCard.NbTimesInNotLearnedHeap,
                        oldestCard.Card.FrontSide, oldestCard.Card.BackSide, oldestCard.Card.AdditionalInfo,
                        oldestCard.Card.VersionUtcDate,
                        oldestCard.Card.VersionCreator.UserName,
                oldestCard.Card.TagsInCards.Select(tagInCard => tagInCard.Tag.Name),
                     oldestCard.Card.UsersWithView.Select(userWithView => userWithView.User.UserName),
                   //emptyResultImageModelArray,
                   oldestCard.Card.Images.Select(img => new ResultImageModel(img.ImageId, img.CardSide, dbContext)),
                        heapingAlgorithm,
              0,//          userRatings.ContainsKey(oldestCard.CardId) ? userRatings[oldestCard.CardId] : 0,
                  0,//      averageRatings.ContainsKey(oldestCard.CardId) ? averageRatings[oldestCard.CardId] : 0,
                     0//   countOfUserRatings.ContainsKey(oldestCard.CardId) ? countOfUserRatings[oldestCard.CardId] : 0
                        )
                    );

                    //var expired = listed.Where(resultCard => heapingAlgorithm.HasExpired(resultCard.Heap, resultCard.LastLearnUtcTime));
                    var listed = thisHeapResult.Select(cardInDeck => cardInDeck.CardId).ToList();
                    result.Add($"Got heap {heap} with {listed.Count} cards after filtering on expiration in {chrono.Elapsed}");
                    resultCards.AddRange(listed);
                }

            result.Add($"Result contains {resultCards.Count()} cards");
            return result;
        }
        #region Request and result classes
        public sealed class Request
        {
            public Request(Guid currentUserId, Guid deckId, bool learnModeIsUnknown, IEnumerable<Guid> excludedCardIds, IEnumerable<Guid> excludedTagIds)
            {
                CurrentUserId = currentUserId;
                DeckId = deckId;
                LearnModeIsUnknown = learnModeIsUnknown;
                ExcludedCardIds = excludedCardIds;
                ExcludedTagIds = excludedTagIds;
            }
            public Guid CurrentUserId { get; }
            public Guid DeckId { get; }
            public bool LearnModeIsUnknown { get; }
            public IEnumerable<Guid> ExcludedCardIds { get; }
            public IEnumerable<Guid> ExcludedTagIds { get; }
        }
        public sealed class ResultCard
        {
            public ResultCard(Guid cardId, int heap, DateTime lastLearnUtcTime, DateTime addToDeckUtcTime, int biggestHeapReached, int nbTimesInNotLearnedHeap,
                string frontSide, string backSide, string additionalInfo, DateTime lastChangeUtcTime, string owner, IEnumerable<string> tags, IEnumerable<string> visibleTo,
                IEnumerable<ResultImageModel> images, HeapingAlgorithm heapingAlgorithm, int userRating, double averageRating, int countOfUserRatings)
            {
                DateServices.CheckUTC(lastLearnUtcTime);
                CardId = cardId;
                Heap = heap;
                LastLearnUtcTime = lastLearnUtcTime;
                LastChangeUtcTime = lastChangeUtcTime;
                AddToDeckUtcTime = addToDeckUtcTime;
                BiggestHeapReached = biggestHeapReached;
                NbTimesInNotLearnedHeap = nbTimesInNotLearnedHeap;
                Owner = owner;
                FrontSide = frontSide;
                BackSide = backSide;
                AdditionalInfo = additionalInfo;
                Tags = tags;
                VisibleTo = visibleTo;
                Images = images;
                UserRating = userRating;
                AverageRating = averageRating;
                CountOfUserRatings = countOfUserRatings;
                MoveToHeapExpiryInfos = Enumerable.Range(1, MoveCardToHeap.MaxTargetHeapId)
                    .Where(heapId => heapId != heap)
                    .Select(targetHeapForMove => new MoveToHeapExpiryInfo(targetHeapForMove, heapingAlgorithm.ExpiryUtcDate(targetHeapForMove, lastLearnUtcTime)))
                    .Concat(new[] { new MoveToHeapExpiryInfo(0, DateTime.MinValue.ToUniversalTime()) });
            }
            public Guid CardId { get; }
            public int Heap { get; }
            public DateTime LastLearnUtcTime { get; }
            public DateTime LastChangeUtcTime { get; }
            public DateTime AddToDeckUtcTime { get; }
            public int BiggestHeapReached { get; }
            public int NbTimesInNotLearnedHeap { get; }
            public string FrontSide { get; }
            public string BackSide { get; }
            public string AdditionalInfo { get; }
            public string Owner { get; }
            public int UserRating { get; }
            public double AverageRating { get; }
            public int CountOfUserRatings { get; }
            public IEnumerable<string> Tags { get; }
            public IEnumerable<string> VisibleTo { get; }
            public IEnumerable<ResultImageModel> Images { get; }
            public IEnumerable<MoveToHeapExpiryInfo> MoveToHeapExpiryInfos { get; }
        }
        public sealed class ResultImageModel
        {
            public ResultImageModel(Guid imageId, int cardSide, MemCheckDbContext dbContext)
            {
                ImageId = imageId;
                var img = dbContext.Images.Where(i => i.Id == imageId).Select(i => new { i.Name, i.Owner, i.Description, i.Source }).Single();
                Owner = img.Owner;
                Name = img.Name;
                Description = img.Description;
                Source = img.Source;
                CardSide = cardSide;
            }
            public Guid ImageId { get; }
            public MemCheckUser Owner { get; }
            public string Name { get; }
            public string Description { get; }
            public string Source { get; }
            public int CardSide { get; set; }   //1 = front side ; 2 = back side ; 3 = AdditionalInfo
        }
        public sealed class MoveToHeapExpiryInfo
        {
            public MoveToHeapExpiryInfo(int heapId, DateTime utcExpiryDate)
            {
                HeapId = heapId;
                UtcExpiryDate = utcExpiryDate;
            }
            public int HeapId { get; }
            public DateTime UtcExpiryDate { get; }
        }
        #endregion
    }
}
