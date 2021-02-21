using MemCheck.Basics;
using MemCheck.Domain;
using System;

namespace MemCheck.Application.Heaping
{
    /* Card expires after a number of days based on 2 at the power of the heap number.
     * We add some random salt to this expiry date so that cards tend to scatter.
     * 
     * For example, a card in heap 10 has an expiry date equal to the last learn date + 1024 days (2 pow 10) + random(1024) minutes.
     * 
     * Without the salt, say a user adds these two cards to his deck:
     * - What is the French for Hello?
     * - What is the English for Bonjour?
     * After adding, he will be asked these two unknown cards in row.
     * As long as he knows the answer, these two will appear grouped in the heaps, climbing up.
     * With salt, we increase the chance that they mix with other cards.
     * 
     * We may find a better name for this class, but please do not change the id of an algorithm (impacts: changes the algorithm of users, breaks the textual resources in DecksController.xx.resx).
     */
    internal sealed class DefaultHeapingAlgorithm : HeapingAlgorithm
    {
        public const int ID = Deck.DefaultHeapingAlgorithmId;
        public override int Id => ID;
        protected override DateTime GetExpiryUtcDate(int currentHeap, DateTime lastLearnUtcTime)
        {
            var nbDaysForExpiration = (int)Math.Pow(2, currentHeap);
            var salt = Randomizer.Next(nbDaysForExpiration);
            return lastLearnUtcTime.AddDays(nbDaysForExpiration).AddMinutes(salt);
        }
    }
}
