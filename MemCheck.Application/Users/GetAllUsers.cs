using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading.Tasks;

namespace MemCheck.Application.Users
{
    public sealed class GetAllUsers
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        private readonly IRoleChecker roleChecker;
        #endregion
        public GetAllUsers(MemCheckDbContext dbContext, IRoleChecker roleChecker)
        {
            this.dbContext = dbContext;
            this.roleChecker = roleChecker;
        }
        public async Task<ResultModel> RunAsync(Request request)
        {
            await request.CheckValidityAsync(dbContext, roleChecker);

            var users = dbContext.Users.AsNoTracking().Where(user => EF.Functions.Like(user.UserName, $"%{request.Filter}%")).OrderBy(user => user.UserName);

            var totalCount = users.Count();
            var pageCount = (int)Math.Ceiling((double)totalCount / request.PageSize);
            var pageEntries = await users.Skip((request.PageNo - 1) * request.PageSize).Take(request.PageSize).ToListAsync();

            var resultUsers = new List<ResultUserModel>();
            foreach (var user in pageEntries)
            {
                var roles = await roleChecker.GetRolesAsync(user);
                resultUsers.Add(new ResultUserModel(user.UserName, user.Id, string.Join(',', roles), user.Email, user.MinimumCountOfDaysBetweenNotifs, user.LastNotificationUtcDate));
            }
            return new ResultModel(totalCount, pageCount, resultUsers);
        }
        #region Request and result classes
        public sealed record Request(Guid UserId, int PageSize, int PageNo, string Filter)
        {
            public const int MaxPageSize = 100;
            public async Task CheckValidityAsync(MemCheckDbContext dbContext, IRoleChecker roleChecker)
            {
                QueryValidationHelper.CheckNotReservedGuid(UserId);
                if (PageSize < 1 || PageSize > MaxPageSize)
                    throw new InvalidOperationException($"Invalid page size: {PageSize}");
                if (PageNo < 1)
                    throw new InvalidOperationException($"Invalid page index: {PageNo}");
                var user = await dbContext.Users.SingleAsync(u => u.Id == UserId);
                if (!await roleChecker.UserIsAdminAsync(user))
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
            public ResultUserModel(string userName, Guid userId, string roles, string email, int notifInterval, DateTime lastNotifUtcDate)
            {
                UserName = userName;
                UserId = userId;
                Roles = roles;
                Email = email;
                NotifInterval = notifInterval;
                LastNotifUtcDate = lastNotifUtcDate;
            }
            public string UserName { get; }
            public Guid UserId { get; }
            public string Roles { get; }
            public string Email { get; }
            public int NotifInterval { get; }
            public DateTime LastNotifUtcDate { get; }
        }
        #endregion
    }
}

