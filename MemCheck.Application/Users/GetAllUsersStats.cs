using MemCheck.Application.QueryValidation;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Users;

public sealed class GetAllUsersStats : RequestRunner<GetAllUsersStats.Request, GetAllUsersStats.ResultModel>
{
    public GetAllUsersStats(CallContext callContext) : base(callContext)
    {
    }
    protected override async Task<ResultWithMetrologyProperties<ResultModel>> DoRunAsync(Request request)
    {
        var normalizedFilter = request.Filter.ToUpperInvariant();
        var users = DbContext.Users.AsNoTracking().Where(user => user.NormalizedUserName != null && user.NormalizedUserName.Contains(normalizedFilter)).OrderBy(user => user.UserName);

        var totalCount = users.Count();
        var pageCount = (int)Math.Ceiling((double)totalCount / request.PageSize);
        var pageEntries = await users.Skip((request.PageNo - 1) * request.PageSize).Take(request.PageSize).ToListAsync();

        var resultUsers = new List<ResultUserModel>();
        foreach (var user in pageEntries)
        {
            var roles = await RoleChecker.GetRolesAsync(user);
            var decks = DbContext.Decks.AsNoTracking().Where(deck => deck.Owner.Id == user.Id).Select(deck => new ResultDeckModel(deck.Description, deck.CardInDecks.Count));
            resultUsers.Add(new ResultUserModel(user.GetUserName(), user.Id, string.Join(',', roles), user.GetEmail(), user.MinimumCountOfDaysBetweenNotifs, user.LastNotificationUtcDate, user.LastSeenUtcDate, user.RegistrationUtcDate, decks));
        }
        var result = new ResultModel(totalCount, pageCount, resultUsers);
        return new ResultWithMetrologyProperties<ResultModel>(result,
            ("LoggedUser", request.UserId.ToString()),
            IntMetric("PageSize", request.PageSize),
            IntMetric("PageNo", request.PageNo),
            IntMetric("FilterLength", request.Filter.Length),
            IntMetric("TotalCount", result.TotalCount),
            IntMetric("PageCount", result.PageCount),
            IntMetric("ResultCount", result.Users.Count()));
    }
    #region Request & Result
    public sealed record Request(Guid UserId, int PageSize, int PageNo, string Filter) : IRequest
    {
        public const int MaxPageSize = 100;
        public async Task CheckValidityAsync(CallContext callContext)
        {
            QueryValidationHelper.CheckNotReservedGuid(UserId);
            if (PageSize is < 1 or > MaxPageSize)
                throw new InvalidOperationException($"Invalid page size: {PageSize}");
            if (PageNo < 1)
                throw new InvalidOperationException($"Invalid page index: {PageNo}");
            var user = await callContext.DbContext.Users.SingleAsync(u => u.Id == UserId);
            if (!await callContext.RoleChecker.UserIsAdminAsync(user))
                throw new InvalidOperationException($"User not admin: {user.UserName}");
        }
    }
    public sealed class ResultModel
    {
        public ResultModel(int totalCount, int pageCount, IEnumerable<ResultUserModel> users)
        {
            TotalCount = totalCount;
            PageCount = pageCount;
            Users = users;
        }
        public int TotalCount { get; }
        public int PageCount { get; }
        public IEnumerable<ResultUserModel> Users { get; }
    }
    public sealed class ResultUserModel
    {
        public ResultUserModel(string userName, Guid userId, string roles, string email, int notifInterval, DateTime lastNotifUtcDate, DateTime lastSeenUtcDate, DateTime registrationUtcDate, IEnumerable<ResultDeckModel> decks)
        {
            UserName = userName;
            UserId = userId;
            Roles = roles;
            Email = email;
            NotifInterval = notifInterval;
            LastNotifUtcDate = lastNotifUtcDate;
            LastSeenUtcDate = lastSeenUtcDate;
            RegistrationUtcDate = registrationUtcDate;
            Decks = decks.ToImmutableArray();
        }
        public string UserName { get; }
        public Guid UserId { get; }
        public string Roles { get; }
        public string Email { get; }
        public int NotifInterval { get; }
        public DateTime LastNotifUtcDate { get; }
        public DateTime LastSeenUtcDate { get; }
        public DateTime RegistrationUtcDate { get; }
        public ImmutableArray<ResultDeckModel> Decks { get; }
    }
    public sealed record ResultDeckModel(string Name, int CardCount);
    #endregion
}

