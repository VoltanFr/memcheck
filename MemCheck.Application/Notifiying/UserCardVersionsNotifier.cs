using MemCheck.Database;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Immutable;
using MemCheck.Domain;
using System;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using MemCheck.Application.QueryValidation;
using System.Diagnostics;

namespace MemCheck.Application.Notifying
{
    internal interface IUserCardVersionsNotifier
    {
        public Task<ImmutableArray<CardVersion>> RunAsync(Guid userId);
    }
    internal sealed class UserCardVersionsNotifier : IUserCardVersionsNotifier
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        private readonly List<string> performanceIndicators;
        private readonly DateTime runningUtcDate;
        #endregion
        public UserCardVersionsNotifier(MemCheckDbContext dbContext, List<string> performanceIndicators)
        {
            //Prod constructor
            this.dbContext = dbContext;
            this.performanceIndicators = performanceIndicators;
            runningUtcDate = DateTime.UtcNow;
        }
        public UserCardVersionsNotifier(MemCheckDbContext dbContext, DateTime runningUtcDate)
        {
            //Unit tests constructor
            this.dbContext = dbContext;
            performanceIndicators = new List<string>();
            this.runningUtcDate = runningUtcDate;
        }
        public async Task<ImmutableArray<CardVersion>> RunAsync(Guid userId)
        {
            var chrono = Stopwatch.StartNew();
            var cardVersions = await dbContext.Cards
                .Include(card => card.VersionCreator)
                .Include(card => card.UsersWithView)
                .Join(dbContext.CardNotifications.Where(cardNotif => cardNotif.UserId == userId), card => card.Id, cardNotif => cardNotif.CardId, (card, cardNotif) => new { card, cardNotif })
                .Where(cardAndNotif => cardAndNotif.card.VersionUtcDate > cardAndNotif.cardNotif.LastNotificationUtcDate)
                .ToListAsync();
            performanceIndicators.Add($"{GetType().Name} took {chrono.Elapsed} to list user's registered cards with new versions");

            var result = cardVersions.Select(cardToReport =>
                               new CardVersion(
                                   cardToReport.card.Id,
                                   cardToReport.card.FrontSide,
                                   cardToReport.card.VersionCreator.UserName,
                                   cardToReport.card.VersionUtcDate,
                                   cardToReport.card.VersionDescription,
                                   CardVisibilityHelper.CardIsVisibleToUser(userId, cardToReport.card.UsersWithView)
                               )
                        ).ToImmutableArray();

            chrono.Restart();
            foreach (var cardVersion in cardVersions)
                cardVersion.cardNotif.LastNotificationUtcDate = runningUtcDate;
            await dbContext.SaveChangesAsync();
            performanceIndicators.Add($"{GetType().Name} took {chrono.Elapsed} to update user's registered cards last notif date");

            return result;
        }
    }
}