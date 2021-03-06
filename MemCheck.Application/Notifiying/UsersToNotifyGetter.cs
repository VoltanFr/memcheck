﻿using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace MemCheck.Application.Notifying
{
    //The information reported by this class is only based on the subscriptions and the last notification date
    //ie, it does not take care of whether or not there are actually changes to notify
    internal interface IUsersToNotifyGetter
    {
        public ImmutableArray<MemCheckUser> Run(DateTime? now = null);
    }
    internal sealed class UsersToNotifyGetter : IUsersToNotifyGetter
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        private readonly List<string> performanceIndicators;
        #endregion
        public UsersToNotifyGetter(MemCheckDbContext dbContext, List<string>? performanceIndicators = null)
        {
            this.dbContext = dbContext;
            this.performanceIndicators = performanceIndicators ?? new List<string>();
        }
        public ImmutableArray<MemCheckUser> Run(DateTime? now = null)
        {
            now ??= DateTime.UtcNow;
            var chrono = Stopwatch.StartNew();
            var userList = dbContext.Users.Where(user => user.MinimumCountOfDaysBetweenNotifs > 0 && EF.Functions.DateDiffHour(user.LastNotificationUtcDate, now) >= user.MinimumCountOfDaysBetweenNotifs * 24);
            //var userList = dbContext.Users.Where(user => user.MinimumCountOfDaysBetweenNotifs > 0;
            //Using DateDiffDay is not suitable because it counts the number of **day boundaries crossed** between the startDate and endDate
            var result = userList.ToImmutableArray();
            performanceIndicators.Add($"{GetType().Name} took {chrono.Elapsed} to list user's card subscriptions");
            return result;
        }
    }
}