using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading.Tasks;

namespace MemCheck.Application
{
    public sealed class GetAllUsers
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        private readonly UserManager<MemCheckUser> userManager;
        #endregion
        public GetAllUsers(MemCheckDbContext dbContext, UserManager<MemCheckUser> userManager)
        {
            this.dbContext = dbContext;
            this.userManager = userManager;
        }
        public async Task<ResultModel> RunAsync(Request request)
        {
            await request.CheckValidityAsync(userManager);

            var users = dbContext.Users.Where(user => EF.Functions.Like(user.UserName, $"%{request.Filter}%")).OrderBy(user => user.UserName);

            var totalCount = users.Count();
            var pageCount = (int)Math.Ceiling(((double)totalCount) / request.PageSize);
            var pageEntries = await users.Skip((request.PageNo - 1) * request.PageSize).Take(request.PageSize).ToListAsync();

            var resultUsers = new List<ResultUserModel>();
            foreach (var user in pageEntries)
            {
                var roles = await userManager.GetRolesAsync(user);
                resultUsers.Add(new ResultUserModel(user.UserName, string.Join(',', roles), user.Email));
            }
            return new ResultModel(totalCount, pageCount, resultUsers);
        }
        #region Request and result classes
        public sealed class Request
        {
            public Request(MemCheckUser currentUser, int pageSize, int pageNo, string filter)
            {
                CurrentUser = currentUser;
                PageSize = pageSize;
                PageNo = pageNo;
                Filter = filter;
            }
            public MemCheckUser CurrentUser { get; }
            public int PageSize { get; }
            public int PageNo { get; }
            public string Filter { get; }
            public async Task CheckValidityAsync(UserManager<MemCheckUser> userManager)
            {
                if (QueryValidationHelper.IsReservedGuid(CurrentUser.Id))
                    throw new RequestInputException($"Invalid user id '{CurrentUser.Id}'");
                if (PageSize < 0 || PageSize > 100)
                    throw new RequestInputException($"Invalid page size: {PageSize}");
                if (PageNo < 0)
                    throw new RequestInputException($"Invalid page index: {PageNo}");
                if (!await userManager.IsInRoleAsync(CurrentUser, "Admin"))
                    throw new SecurityException($"User not admin: {CurrentUser.UserName}");
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
            public ResultUserModel(string userName, string roles, string email)
            {
                UserName = userName;
                Roles = roles;
                Email = email;
            }
            public string UserName { get; }
            public string Roles { get; }
            public string Email { get; }
        }
        #endregion
    }
}

