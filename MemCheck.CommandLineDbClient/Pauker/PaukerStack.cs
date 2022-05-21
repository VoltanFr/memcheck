using System;
using System.Collections.Generic;

namespace MemCheck.CommandLineDbClient.Pauker;

internal class PaukerStack
{
    public PaukerStack(string name, List<PaukerCard> cards)
    {
        Name = name;
        Cards = cards;
    }
    public string Name { get; }
    internal bool RemoveCard(PaukerCard card, bool excludingFirst)
    {
        var indicesToDelete = new List<int>();

        for (int i = 0; i < Cards.Count; i++)
            if (Cards[i].Equals(card))
                indicesToDelete.Add(i);

        if (indicesToDelete.Count == 0)
            return false;

        if (excludingFirst)
        {
            int indexToKeep = 0;
            long smallestLearnedTimeStamp = long.MaxValue;
            foreach (int i in indicesToDelete)
            {
                var learned = Cards[i].Front.LearnedTimestamp;
                if (!learned.HasValue || (learned.Value < smallestLearnedTimeStamp))
                    indexToKeep = i;
            }
            indicesToDelete.Remove(indexToKeep);
        }

        for (int i = indicesToDelete.Count - 1; i >= 0; i--)
        {
            Console.WriteLine("Deleting card with front '{0}'", card.Front.Text);
            Cards.RemoveAt(indicesToDelete[i]);
        }

        return true;
    }
    public List<PaukerCard> Cards { get; }
}
