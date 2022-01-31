using MemCheck.Application.Heaping;
using MemCheck.Application.Tests.Helpers;
using MemCheck.Basics;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards
{
    [TestClass()]
    public class GetRemainingCardsInLessonTests
    {
        [TestMethod()]
        public async Task UserNotLoggedIn()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);

            using var dbContext = new MemCheckDbContext(db);
            var request = new GetRemainingCardsInLesson.Request(Guid.Empty, deck, RandomHelper.Bool(), Array.Empty<Guid>());
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetRemainingCardsInLesson(dbContext.AsCallContext()).RunAsync(request));
        }
        [TestMethod()]
        public async Task UserDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);

            using var dbContext = new MemCheckDbContext(db);
            var request = new GetRemainingCardsInLesson.Request(Guid.NewGuid(), deck, RandomHelper.Bool(), Array.Empty<Guid>());
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetRemainingCardsInLesson(dbContext.AsCallContext()).RunAsync(request));
        }
        [TestMethod()]
        public async Task DeckDoesNotExist()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            var request = new GetRemainingCardsInLesson.Request(user, Guid.NewGuid(), RandomHelper.Bool(), Array.Empty<Guid>());
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetRemainingCardsInLesson(dbContext.AsCallContext()).RunAsync(request));
        }
        [TestMethod()]
        public async Task UserNotOwner()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);
            var otherUser = await UserHelper.CreateInDbAsync(db);

            using var dbContext = new MemCheckDbContext(db);
            var request = new GetRemainingCardsInLesson.Request(otherUser, deck, RandomHelper.Bool(), Array.Empty<Guid>());
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetRemainingCardsInLesson(dbContext.AsCallContext()).RunAsync(request));
        }
        [TestMethod()]
        public async Task EmptyDeck()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user, algorithmId: DefaultHeapingAlgorithm.ID);

            using var dbContext = new MemCheckDbContext(db);
            var request = new GetRemainingCardsInLesson.Request(user, deck, RandomHelper.Bool(), Array.Empty<Guid>());
            var result = await new GetRemainingCardsInLesson(dbContext.AsCallContext(), new DateTime(2000, 1, 2)).RunAsync(request);
            Assert.AreEqual(0, result.Count);
        }
        [TestMethod()]
        public async Task OneCardUnknown()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user, algorithmId: UnitTestsHeapingAlgorithm.ID);
            await DeckHelper.AddNewCardAsync(db, deck, CardInDeck.UnknownHeap);

            using var dbContext = new MemCheckDbContext(db);

            var expiredRequest = new GetRemainingCardsInLesson.Request(user, deck, false, Array.Empty<Guid>());
            var expiredResult = await new GetRemainingCardsInLesson(dbContext.AsCallContext()).RunAsync(expiredRequest);
            Assert.AreEqual(0, expiredResult.Count);

            var unknownRequest = new GetRemainingCardsInLesson.Request(user, deck, true, Array.Empty<Guid>());
            var unknownResult = await new GetRemainingCardsInLesson(dbContext.AsCallContext()).RunAsync(unknownRequest);
            Assert.AreEqual(1, unknownResult.Count);
        }
        [TestMethod()]
        public async Task OneCardNonExpired()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user, algorithmId: UnitTestsHeapingAlgorithm.ID);
            var lastLearnUtcTime = RandomHelper.Date();
            await DeckHelper.AddNewCardAsync(db, deck, 4, lastLearnUtcTime: lastLearnUtcTime);

            using var dbContext = new MemCheckDbContext(db);

            var expiredRequest = new GetRemainingCardsInLesson.Request(user, deck, false, Array.Empty<Guid>());
            var expiredResult = await new GetRemainingCardsInLesson(dbContext.AsCallContext(), lastLearnUtcTime.AddDays(1).AddSeconds(-1)).RunAsync(expiredRequest);
            Assert.AreEqual(0, expiredResult.Count);

            var unknownRequest = new GetRemainingCardsInLesson.Request(user, deck, true, Array.Empty<Guid>());
            var unknownResult = await new GetRemainingCardsInLesson(dbContext.AsCallContext(), lastLearnUtcTime.AddDays(1).AddSeconds(-1)).RunAsync(unknownRequest);
            Assert.AreEqual(0, unknownResult.Count);
        }
        [TestMethod()]
        public async Task OneCardExpired()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user);
            var lastLearnUtcTime = RandomHelper.Date();
            await DeckHelper.AddNewCardAsync(db, deck, 1, lastLearnUtcTime);

            using var dbContext = new MemCheckDbContext(db);

            var expiredRequest = new GetRemainingCardsInLesson.Request(user, deck, false, Array.Empty<Guid>());
            var expiredResult = await new GetRemainingCardsInLesson(dbContext.AsCallContext(), lastLearnUtcTime.AddDays(1)).RunAsync(expiredRequest);
            Assert.AreEqual(1, expiredResult.Count);

            var unknownRequest = new GetRemainingCardsInLesson.Request(user, deck, true, Array.Empty<Guid>());
            var unknownResult = await new GetRemainingCardsInLesson(dbContext.AsCallContext(), lastLearnUtcTime.AddDays(1)).RunAsync(unknownRequest);
            Assert.AreEqual(0, unknownResult.Count);
        }
        [TestMethod()]
        public async Task ExpiredAndNonExpiredCards()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var heapingAlgorithm = new UnitTestsHeapingAlgorithm();
            var deck = await DeckHelper.CreateAsync(db, user, algorithmId: heapingAlgorithm.Id);
            var runDate = RandomHelper.Date();

            var expiredCardCount = 0;
            var unknownCardCount = 0;

            for (int currentCardIndex = 0; currentCardIndex < 100; currentCardIndex++)
            {
                var heap = RandomHelper.Heap();
                var lastLearnDate = runDate.AddDays(heap).AddDays(Randomizer.Next(-10, 10));
                await DeckHelper.AddNewCardAsync(db, deck, heap, lastLearnDate);
                if (heap == 0)
                    unknownCardCount++;
                else
                {
                    if (heapingAlgorithm.ExpiryUtcDate(heap, lastLearnDate) <= runDate)
                        expiredCardCount++;
                }
            }

            using var dbContext = new MemCheckDbContext(db);

            var expiredRequest = new GetRemainingCardsInLesson.Request(user, deck, false, Array.Empty<Guid>());
            var expiredResult = await new GetRemainingCardsInLesson(dbContext.AsCallContext(), runDate).RunAsync(expiredRequest);
            Assert.AreEqual(expiredCardCount, expiredResult.Count);

            var unknownRequest = new GetRemainingCardsInLesson.Request(user, deck, true, Array.Empty<Guid>());
            var unknownResult = await new GetRemainingCardsInLesson(dbContext.AsCallContext(), runDate).RunAsync(unknownRequest);
            Assert.AreEqual(unknownCardCount, unknownResult.Count);
        }
        [TestMethod()]
        public async Task WithIgnoredTags()
        {
            var db = DbHelper.GetEmptyTestDB();
            var user = await UserHelper.CreateInDbAsync(db);
            var deck = await DeckHelper.CreateAsync(db, user, algorithmId: UnitTestsHeapingAlgorithm.ID);
            var tagToIgnore = await TagHelper.CreateAsync(db);
            var otherTag = await TagHelper.CreateAsync(db);
            var runDate = RandomHelper.Date();

            //Add unkown cards
            await DeckHelper.AddNewCardAsync(db, deck, CardInDeck.UnknownHeap);
            await DeckHelper.AddNewCardAsync(db, deck, CardInDeck.UnknownHeap, tagIds: tagToIgnore.AsArray());
            await DeckHelper.AddNewCardAsync(db, deck, CardInDeck.UnknownHeap, tagIds: otherTag.AsArray());
            await DeckHelper.AddNewCardAsync(db, deck, CardInDeck.UnknownHeap, tagIds: new[] { tagToIgnore, otherTag });

            //Add non expired cards
            await DeckHelper.AddNewCardAsync(db, deck, RandomHelper.Heap(true), runDate.AddHours(Randomizer.Next(-23, -1)));
            await DeckHelper.AddNewCardAsync(db, deck, RandomHelper.Heap(true), runDate.AddHours(Randomizer.Next(-23, -1)), tagIds: tagToIgnore.AsArray());
            await DeckHelper.AddNewCardAsync(db, deck, RandomHelper.Heap(true), runDate.AddHours(Randomizer.Next(-23, -1)), tagIds: otherTag.AsArray());
            await DeckHelper.AddNewCardAsync(db, deck, RandomHelper.Heap(true), runDate.AddHours(Randomizer.Next(-23, -1)), tagIds: new[] { tagToIgnore, otherTag });

            //Add expired cards
            await DeckHelper.AddNewCardAsync(db, deck, 1, runDate.AddDays(-2));
            await DeckHelper.AddNewCardAsync(db, deck, 1, runDate.AddDays(-2), tagIds: tagToIgnore.AsArray());
            await DeckHelper.AddNewCardAsync(db, deck, 1, runDate.AddDays(-2), tagIds: otherTag.AsArray());
            await DeckHelper.AddNewCardAsync(db, deck, 1, runDate.AddDays(-2), tagIds: new[] { tagToIgnore, otherTag });

            using var dbContext = new MemCheckDbContext(db);

            var expiredRequest = new GetRemainingCardsInLesson.Request(user, deck, false, tagToIgnore.AsArray());
            var expiredResult = await new GetRemainingCardsInLesson(dbContext.AsCallContext(), runDate).RunAsync(expiredRequest);
            Assert.AreEqual(2, expiredResult.Count);

            var unknownRequest = new GetRemainingCardsInLesson.Request(user, deck, true, tagToIgnore.AsArray());
            var unknownResult = await new GetRemainingCardsInLesson(dbContext.AsCallContext(), runDate).RunAsync(unknownRequest);
            Assert.AreEqual(2, unknownResult.Count);
        }
    }
}
