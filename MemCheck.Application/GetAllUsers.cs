using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application
{
    public sealed class GetAllUsers
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public GetAllUsers(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task<ResultModel> RunAsync(Request request)
        {
            await request.CheckValidityAsync(dbContext);

            var users = dbContext.Users.Where(user => EF.Functions.Like(user.UserName, $"%{request.Filter}%")).OrderBy(user => user.UserName);
            var totalCount = users.Count();
            var pageCount = (int)Math.Ceiling(((double)totalCount) / request.PageSize);
            var pageEntries = users.Skip((request.PageNo - 1) * request.PageSize).Take(request.PageSize);

            return new ResultModel(totalCount, pageCount, pageEntries.Select(user => new ResultUserModel(user.UserName)));
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
            public async Task CheckValidityAsync(MemCheckDbContext dbContext)
            {
                if (QueryValidationHelper.IsReservedGuid(CurrentUser.Id))
                    throw new RequestInputException($"Invalid user id '{CurrentUser.Id}'");
                if (PageSize < 0 || PageSize > 100)
                    throw new RequestInputException($"Invalid page size: {PageSize}");
                if (PageNo < 0)
                    throw new RequestInputException($"Invalid page index: {PageNo}");

                await Task.CompletedTask;
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
            public ResultUserModel(string userName)
            {
                UserName = userName;
            }
            public string UserName { get; } = null!;
        }
        #endregion
    }
}

