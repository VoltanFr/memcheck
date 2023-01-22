using MemCheck.Application.QueryValidation;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Users;

public sealed class GetAdminEmailAddesses : RequestRunner<GetAdminEmailAddesses.Request, GetAdminEmailAddesses.ResultModel>
{
    public GetAdminEmailAddesses(CallContext callContext) : base(callContext)
    {
    }
    protected override async Task<ResultWithMetrologyProperties<ResultModel>> DoRunAsync(Request request)
    {
        var users = await DbContext.Users.AsNoTracking().ToListAsync();
        var admins = users.Where(u => RoleChecker.UserIsAdminAsync(u).Result).Select(u => new ResultUserModel(u.GetUserName(), u.GetEmail()));
        var result = new ResultModel(admins);
        return new ResultWithMetrologyProperties<ResultModel>(result, ("LoggedUser", request.UserId.ToString()), IntMetric("ResultCount", result.Users.Count()));
    }
    #region Request & Result
    public sealed record Request(Guid UserId) : IRequest
    {
        public async Task CheckValidityAsync(CallContext callContext)
        {
            QueryValidationHelper.CheckNotReservedGuid(UserId);
            var user = await callContext.DbContext.Users.SingleAsync(u => u.Id == UserId);
            if (!await callContext.RoleChecker.UserIsAdminAsync(user))
                throw new InvalidOperationException($"User not admin: {user.UserName}");
        }
    }
    public sealed class ResultModel
    {
        public ResultModel(IEnumerable<ResultUserModel> users)
        {
            Users = users;
        }
        public IEnumerable<ResultUserModel> Users { get; }
    }
    public sealed record ResultUserModel(string Name, string Email);
    #endregion
}
