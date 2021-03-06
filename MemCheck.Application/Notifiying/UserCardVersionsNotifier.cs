﻿using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

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
        #region Private methods
        private Guid? GetCardVersionOn(Guid cardId, DateTime utc)
        {
            var resultVersion = dbContext.CardPreviousVersions.AsNoTracking()
                .Where(cardVersion => cardVersion.Card == cardId && cardVersion.VersionUtcDate <= utc)
                .OrderByDescending(cardVersion => cardVersion.VersionUtcDate)
                .FirstOrDefault();
            return resultVersion?.Id;
        }
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

            chrono.Restart();
            var result = cardVersions.Select(cardToReport =>
                              new CardVersion(
                                  cardToReport.card.Id,
                                  cardToReport.card.FrontSide,
                                  cardToReport.card.VersionCreator.UserName,
                                  cardToReport.card.VersionUtcDate,
                                  cardToReport.card.VersionDescription,
                                  CardVisibilityHelper.CardIsVisibleToUser(userId, cardToReport.card.UsersWithView),
                                  GetCardVersionOn(cardToReport.card.Id, cardToReport.cardNotif.LastNotificationUtcDate)
                              )
                        ).ToImmutableArray();
            performanceIndicators.Add($"{GetType().Name} took {chrono.Elapsed} to create the result list with getting card version on last notif");

            chrono.Restart();
            foreach (var cardVersion in cardVersions)
                cardVersion.cardNotif.LastNotificationUtcDate = runningUtcDate;
            await dbContext.SaveChangesAsync();
            performanceIndicators.Add($"{GetType().Name} took {chrono.Elapsed} to update user's registered cards last notif date");

            return result;
        }
    }
}