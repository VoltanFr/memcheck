﻿using MemCheck.Database;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Immutable;
using MemCheck.Domain;
using System;
using Microsoft.EntityFrameworkCore;
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
        private readonly DateTime runningUtcDate;
        #endregion
        public UserCardVersionsNotifier(MemCheckDbContext dbContext, DateTime? runningUtcDate = null)
        {
            this.dbContext = dbContext;
            this.runningUtcDate = runningUtcDate ?? DateTime.UtcNow;
        }
        public async Task<ImmutableArray<CardVersion>> RunAsync(Guid userId)
        {
            var cardVersions = dbContext.Cards.Include(card => card.UsersWithView)
                .Join(
                dbContext.CardNotifications.Where(cardNotif => cardNotif.UserId == userId),
                card => card.Id,
                cardNotif => cardNotif.CardId,
                (card, cardNotif) => new { card, cardNotif }
                )
                .Where(cardAndNotif => cardAndNotif.card.VersionUtcDate > cardAndNotif.cardNotif.LastNotificationUtcDate);

            foreach (var cardVersion in cardVersions)
                cardVersion.cardNotif.LastNotificationUtcDate = runningUtcDate;

            var result = cardVersions.Select(cardToReport =>
                               new CardVersion(
                                   cardToReport.card.Id,
                                   cardToReport.card.FrontSide,
                                   cardToReport.card.VersionCreator.UserName,
                                   cardToReport.card.VersionUtcDate,
                                   cardToReport.card.VersionDescription,
                                   !cardToReport.card.UsersWithView.Any() || cardToReport.card.UsersWithView.Any(u => u.UserId == userId)
                               )
                        ).ToImmutableArray();

            await dbContext.SaveChangesAsync();

            return result;
        }
    }
}