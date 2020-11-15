using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MemCheck.Application
{
    public sealed class GetCardsInDeck
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public GetCardsInDeck(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public IEnumerable<ViewModel> Run(Guid deckId)
        {
            var deck = dbContext.Decks.Where(deck => deck.Id == deckId)
                .Include(deck => deck.CardInDecks)
                .ThenInclude(cardInDeck => cardInDeck.Card)
                .SingleOrDefault();

            if (deck == null)
                return Array.Empty<ViewModel>();

            return deck.CardInDecks.Select(cardInDeck => new ViewModel(
                cardInDeck.CardId,
                cardInDeck.CurrentHeap,
                cardInDeck.LastLearnUtcTime,
                cardInDeck.BiggestHeapReached,
                cardInDeck.NbTimesInNotLearnedHeap,
                cardInDeck.Card.FrontSide,
                cardInDeck.Card.BackSide
                //tags=
                ));
        }
        public sealed class ViewModel
        //To be updated: this class is not allowed to transform the data, eg ToString("g"). This is the responsibility of the GUI, not the APplication (explained in Readme.md)
        {
            #region Fields
            private readonly Guid cardId;
            private readonly int currentHeap;
            private readonly string lastLearnTime;
            private readonly int biggestHeapReached;
            private readonly string nbTimesInNotLearnedHeap;
            private readonly string frontSide;
            private readonly string backSide;
            #endregion
            public ViewModel(Guid cardId, int currentHeap, DateTime lastLearnUtcTime, int biggestHeapReached, int nbTimesInNotLearnedHeap, string frontSide, string backSide/*, IEnumerable<string> tags*/)
            {
                DateServices.CheckUTC(lastLearnUtcTime);
                this.cardId = cardId;
                this.currentHeap = currentHeap;
                this.lastLearnTime = lastLearnUtcTime.ToString("g");
                this.biggestHeapReached = biggestHeapReached;
                this.frontSide = frontSide;
                this.backSide = backSide;
                this.nbTimesInNotLearnedHeap = nbTimesInNotLearnedHeap.ToString();
            }
            public Guid CardId { get => cardId; }
            public string FrontSide { get => frontSide; }
            public string BackSide { get => backSide; }
            public int CurrentHeap { get => currentHeap; }
            public string LastLearnTime { get => lastLearnTime; }
            public string NbTimesInNotLearnedHeap { get => nbTimesInNotLearnedHeap; }
            public int BiggestHeapReached { get => biggestHeapReached; }
        }
    }
}
