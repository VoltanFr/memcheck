using System;
using System.Collections.Generic;

namespace MemCheck.CommandLineDbClient.Pauker
{
    class PaukerStack
    {
        private readonly string name;
        private readonly List<PaukerCard> cards;
        public PaukerStack(string name, List<PaukerCard> cards)
        {
            this.name = name;
            this.cards = cards;
        }
        public string Name
        {
            get
            {
                return name;
            }
        }
        internal bool RemoveCard(PaukerCard card, bool excludingFirst)
        {
            var indicesToDelete = new List<int>();

            for (int i = 0; i < cards.Count; i++)
                if (cards[i].Equals(card))
                    indicesToDelete.Add(i);

            if (indicesToDelete.Count == 0)
                return false;

            if (excludingFirst)
            {
                int indexToKeep = 0;
                long smallestLearnedTimeStamp = long.MaxValue;
                foreach (int i in indicesToDelete)
                {
                    var learned = cards[i].Front.LearnedTimestamp;
                    if (!learned.HasValue || (learned.Value < smallestLearnedTimeStamp))
                        indexToKeep = i;
                }
                indicesToDelete.Remove(indexToKeep);
            }

            for (int i = indicesToDelete.Count - 1; i >= 0; i--)
            {
                Console.WriteLine("Deleting card with front '{0}'", card.Front.Text);
                cards.RemoveAt(indicesToDelete[i]);
            }

            return true;
        }
        public List<PaukerCard> Cards => cards;
    }
}
