using MemCheck.Domain;
using System;
using System.Security.Cryptography;

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
     *
     * Here is the table of power of 2 (info about duration using this number as days), and info about duration using this number * 10 as salt in minutes
     *  1 ->     2            -> 20 minutes
     *  2 ->     4            -> 40 minutes
     *  3 ->     8            -> 1 hour
     *  4 ->    16            -> 3 hours
     *  5 ->    32 (1 month)  -> 5 hours
     *  6 ->    64 (2 months) -> 0.5 day
     *  7 ->   128 (4 months) -> 1 day
     *  8 ->   256 (8 months) -> 2 days
     *  9 ->   512 (1.5 year) -> 3 days
     * 10 ->  1024 (3 years)  -> 7 days
     * 11 ->  2048 (5 years)  -> 14 days
     * 12 ->  4096 (11 years) -> 28 days
     * 13 ->  8192 (22 years) -> 56 days
     * 14 -> 16384 (45 years) -> 113 days
     * 15 -> 32768 (91 years) -> 227 days
     */
    internal sealed class DefaultHeapingAlgorithm : HeapingAlgorithm
    {
        public const int ID = Deck.DefaultHeapingAlgorithmId;
        public override int Id => ID;
        protected override DateTime GetExpiryUtcDate(int currentHeap, DateTime lastLearnUtcTime)
        {
            var nbDaysForExpiration = (int)Math.Pow(2, currentHeap);
            var salt = RandomNumberGenerator.GetInt32(nbDaysForExpiration * 10);
            return lastLearnUtcTime.AddDays(nbDaysForExpiration).AddMinutes(salt);
        }
    }
}
