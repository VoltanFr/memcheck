using System;

namespace MemCheck.Domain
{
    public sealed class CardInDeck
    {
        public const int UnknownHeap = 0;
        public const int MaxHeapValue = 15;
        public static readonly DateTime NeverLearntLastLearnTime = DateTime.MinValue.ToUniversalTime();

        public Guid CardId { get; set; }
        public Card Card { get; set; } = null!;

        public Guid DeckId { get; set; }
        public Deck Deck { get; set; } = null!;

        public int CurrentHeap { get; set; }    //Legal values are between 0 and MaxHeapValue
        public DateTime LastLearnUtcTime { get; set; }  //For an unknown card, this is the date on which it is moved to the unknown heap (including if it was already in this heap). For an unknown card which has never been learnt, this is NeverLearntLastLearnTime
        public DateTime AddToDeckUtcTime { get; set; }  //Date on which this card was added to this deck
        public int NbTimesInNotLearnedHeap { get; set; } //Count of times this card is **moved** to unknown heap. In other words, when an unknown card is unknown again, this count is not incremented
        public int BiggestHeapReached { get; set; }
    }
}
