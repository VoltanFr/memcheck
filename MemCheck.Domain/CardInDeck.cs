using System;

namespace MemCheck.Domain
{
    public sealed class CardInDeck
    {
        public const int MaxHeapValue = 15;

        public Guid CardId { get; set; }
        public Card Card { get; set; } = null!;

        public Guid DeckId { get; set; }
        public Deck Deck { get; set; } = null!;

        public int CurrentHeap { get; set; }    //Legal values are between 0 and MaxHeapValue
        public DateTime LastLearnUtcTime { get; set; }  //For an unknown card, this is the date on which it is moved to the unknown heap (including if it was already in this heap)
        public DateTime AddToDeckUtcTime { get; set; }  //Date on which this card was added to this deck
        public int NbTimesInNotLearnedHeap { get; set; }
        public int BiggestHeapReached { get; set; }
    }
}
