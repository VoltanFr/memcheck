using MemCheck.Basics;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Heaping
{
    public abstract class HeapingAlgorithm
    {
        #region Protected method GetHasExpired
        protected abstract DateTime GetExpiryUtcDate(int currentHeap, DateTime lastLearnUtcTime);    //currentHeap is guaranteed to be > 0    //Please find a better name for this method
        #endregion
        public abstract int Id { get; }
        public DateTime ExpiryUtcDate(int currentHeap, DateTime lastLearnUtcTime)
        {
            DateServices.CheckUTC(lastLearnUtcTime);
            if (currentHeap < 1)
                throw new ArgumentException("card is unknown");
            var result = GetExpiryUtcDate(currentHeap, lastLearnUtcTime);
            DateServices.CheckUTC(result);
            return result;
        }
        public static async Task<HeapingAlgorithm> OfDeckAsync(MemCheckDbContext dbContext, Guid deckId)
        {
            var heapingAlgorithmId = await dbContext.Decks.AsNoTracking().Where(deck => deck.Id == deckId).Select(deck => deck.HeapingAlgorithmId).SingleAsync();
            var heapingAlgorithm = HeapingAlgorithms.Instance.FromId(heapingAlgorithmId);
            return heapingAlgorithm;

        }
    }
}
