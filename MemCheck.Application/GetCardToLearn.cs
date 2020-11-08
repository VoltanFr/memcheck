using MemCheck.Application.Heaping;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Drawing;

namespace MemCheck.Application
{
    public sealed class ObsoletGetCardToLearn
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        #region Private class HeapingAlgoCardRequestWithId
        private sealed class HeapingAlgoCardRequestWithId
        {
            public HeapingAlgoCardRequestWithId(Guid cardId, int currentHeap, DateTime lastLearnUtcTime)
            {
                CardId = cardId;
                CurrentHeap = currentHeap;
                LastLearnUtcTime = lastLearnUtcTime;
            }
            public Guid CardId { get; }
            public int CurrentHeap { get; }
            public DateTime LastLearnUtcTime { get; }
        }
        #endregion
        #region Private methods
        private IQueryable<CardInDeck> GetDeckCardsQuery(Guid deckId)
        {
            return dbContext.CardsInDecks.Where(card => card.DeckId.Equals(deckId)).Include(cardInDeck => cardInDeck.Card.TagsInCards).Include(cardInDeck => cardInDeck.Card.UsersWithView);
        }
        private ResultModel? GetUnknownCard(Guid deckId, Guid cardIdToExclude1, Guid cardIdToExclude2)
        {
            var cardsOfDeck = dbContext.CardsInDecks.Where(card => card.DeckId.Equals(deckId) && card.CardId != cardIdToExclude1 && card.CardId != cardIdToExclude2)
                .Include(cardInDeck => cardInDeck.Card.TagsInCards)
                .Include(cardInDeck => cardInDeck.Card.VersionCreator)
                .Include(cardInDeck => cardInDeck.Card.UsersWithView)
                .Include(cardInDeck => cardInDeck.Card.Images)
                .ThenInclude(imageInCard => imageInCard.Image);
            var resultCards = cardsOfDeck.Where(card => card.CurrentHeap == 0);
            if (!resultCards.Any())
                return null;
            var oldest = resultCards.OrderBy(card => card.LastLearnUtcTime).Take(1);
            return oldest.Select(cardForUser => new ResultModel(
               cardForUser.CardId, cardForUser.CurrentHeap, cardForUser.LastLearnUtcTime, cardForUser.AddToDeckUtcTime,
               cardForUser.BiggestHeapReached, cardForUser.NbTimesInNotLearnedHeap, cardForUser.Card.FrontSide, cardForUser.Card.BackSide, cardForUser.Card.AdditionalInfo,
               cardForUser.Card.VersionUtcDate, cardForUser.Card.VersionCreator.UserName,
               cardForUser.Card.TagsInCards.Select(tagInCard => tagInCard.Tag.Name),
               cardForUser.Card.UsersWithView.Select(userWithView => userWithView.User.UserName),
               cardForUser.Card.Images.Select(img => new ResultImageModel(img))
               )).Single();
        }
        private ResultModel? GetCardToRepeat(Guid deckId, Guid cardIdToExclude1, Guid cardIdToExclude2)
        {
            var heapingAlgorithmId = dbContext.Decks.Where(deck => deck.Id == deckId).Select(deck => deck.HeapingAlgorithmId).Single();
            var heapingAlgorithm = HeapingAlgorithms.Instance.FromId(heapingAlgorithmId);

            var heaps = dbContext.CardsInDecks.Where(cardInDeck => cardInDeck.DeckId == deckId && cardInDeck.CurrentHeap != 0).Select(cardInDeck => cardInDeck.CurrentHeap).Distinct().ToList();

            foreach (var heap in heaps.OrderByDescending(heap => heap))
            {
                var cardsOfHeap = dbContext.CardsInDecks.Where(cardInDeck => cardInDeck.DeckId == deckId && cardInDeck.CurrentHeap == heap && cardInDeck.CardId != cardIdToExclude1 && cardInDeck.CardId != cardIdToExclude2);
                var ordered = cardsOfHeap.OrderBy(cardInDeck => cardInDeck.LastLearnUtcTime);

                if (ordered.Any())
                {
                    var oldest = ordered.Take(1);
                    var withDetails = oldest
                        .Include(cardInDeck => cardInDeck.Card)
                        .Include(cardInDeck => cardInDeck.Card.TagsInCards)
                        .ThenInclude(tagInCard => tagInCard.Tag)
                        .Include(cardInDeck => cardInDeck.Card.UsersWithView)
                        .ThenInclude(userWithViewOnCard => userWithViewOnCard.User)
                        .Include(cardInDeck => cardInDeck.Card.Images)
                        .ThenInclude(imageInCard => imageInCard.Image);
                    var oldestCard = withDetails.Single();

                    if (heapingAlgorithm.HasExpired(oldestCard.CurrentHeap, oldestCard.LastLearnUtcTime))
                        return new ResultModel(oldestCard.CardId, oldestCard.CurrentHeap, oldestCard.LastLearnUtcTime, oldestCard.AddToDeckUtcTime,
                                oldestCard.BiggestHeapReached, oldestCard.NbTimesInNotLearnedHeap,
                                oldestCard.Card.FrontSide, oldestCard.Card.BackSide, oldestCard.Card.AdditionalInfo,
                                oldestCard.Card.VersionUtcDate, oldestCard.Card.VersionCreator.UserName,
                                oldestCard.Card.TagsInCards.Select(tagInCard => tagInCard.Tag.Name),
                                oldestCard.Card.UsersWithView.Select(userWithView => userWithView.User.UserName),
                                oldestCard.Card.Images.Select(img => new ResultImageModel(img)));
                }
            }

            return null;
        }
        #endregion
        public ObsoletGetCardToLearn(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public ResultModel? Run(Guid deckId, bool unknown, Guid cardIdToExclude1, Guid cardIdToExclude2)
        {
            if (unknown)
                return GetUnknownCard(deckId, cardIdToExclude1, cardIdToExclude2);
            return GetCardToRepeat(deckId, cardIdToExclude1, cardIdToExclude2);
        }
        public sealed class ResultModel
        {
            public ResultModel(Guid cardId, int heap, DateTime lastLearnUtcTime, DateTime addToDeckUtcTime, int biggestHeapReached, int nbTimesInNotLearnedHeap,
                string frontSide, string backSide, string additionalInfo, DateTime lastChangeUtcTime, string? owner, IEnumerable<string> tags, IEnumerable<string> visibleTo, IEnumerable<ResultImageModel> images)
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
            public string? Owner { get; }
            public IEnumerable<string> Tags { get; }
            public IEnumerable<string> VisibleTo { get; }
            public IEnumerable<ResultImageModel> Images { get; }
        }
        public sealed class ResultImageModel
        {
            public ResultImageModel(ImageInCard img)
            {
                ImageId = img.ImageId;
                Owner = img.Image.Owner;
                Name = img.Image.Name;
                Description = img.Image.Description;
                Source = img.Image.Source;
                CardSide = img.CardSide;
            }
            public Guid ImageId { get; }
            public MemCheckUser Owner { get; }
            public string Name { get; }
            public string Description { get; }
            public string Source { get; }
            public int CardSide { get; set; }   //1 = front side ; 2 = back side ; 3 = AdditionalInfo
        }
    }
}
