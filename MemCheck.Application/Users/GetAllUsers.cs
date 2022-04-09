using MemCheck.Application.QueryValidation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Users
{
    public sealed class GetAllUsers : RequestRunner<GetAllUsers.Request, GetAllUsers.ResultModel>
    {
        public GetAllUsers(CallContext callContext) : base(callContext)
        {
        }
        protected override async Task<ResultWithMetrologyProperties<ResultModel>> DoRunAsync(Request request)
        {
            var users = DbContext.Users.AsNoTracking().Where(user => EF.Functions.Like(user.UserName, $"%{request.Filter}%")).OrderBy(user => user.UserName);

            var totalCount = users.Count();
            var pageCount = (int)Math.Ceiling((double)totalCount / request.PageSize);
            var pageEntries = await users.Skip((request.PageNo - 1) * request.PageSize).Take(request.PageSize).ToListAsync();

            var resultUsers = new List<ResultUserModel>();
            foreach (var user in pageEntries)
            {
                var roles = await RoleChecker.GetRolesAsync(user);
                resultUsers.Add(new ResultUserModel(user.UserName, user.Id, string.Join(',', roles), user.Email, user.MinimumCountOfDaysBetweenNotifs, user.LastNotificationUtcDate, user.LastSeenUtcDate, user.RegistrationUtcDate));
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
                if (PageSize < 1 || PageSize > MaxPageSize)
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
            public ResultUserModel(string userName, Guid userId, string roles, string email, int notifInterval, DateTime lastNotifUtcDate, DateTime lastSeenUtcDate, DateTime registrationUtcDate)
            {
                UserName = userName;
                UserId = userId;
                Roles = roles;
                Email = email;
                NotifInterval = notifInterval;
                LastNotifUtcDate = lastNotifUtcDate;
                LastSeenUtcDate = lastSeenUtcDate;
                RegistrationUtcDate = registrationUtcDate;
            }
            public string UserName { get; }
            public Guid UserId { get; }
            public string Roles { get; }
            public string Email { get; }
            public int NotifInterval { get; }
            public DateTime LastNotifUtcDate { get; }
            public DateTime LastSeenUtcDate { get; }
            public DateTime RegistrationUtcDate { get; }
        }
        #endregion
    }
}

