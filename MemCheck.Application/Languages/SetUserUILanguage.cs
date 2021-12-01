using MemCheck.Application.QueryValidation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace MemCheck.Application.Languages
{
    public sealed class SetUserUILanguage : RequestRunner<SetUserUILanguage.Request, SetUserUILanguage.Result>
    {
        public SetUserUILanguage(CallContext callContext) : base(callContext)
        {
        }
        protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
        {
            var user = await DbContext.Users.SingleAsync(user => user.Id == request.UserId);
            user.UILanguage = request.CultureName;
            DbContext.SaveChanges();
            return new ResultWithMetrologyProperties<Result>(new Result(), ("CultureName", request.CultureName));
        }
        #region Request & Result
        public sealed record Request(Guid UserId, string CultureName) : IRequest
        {
            public const int MinNameLength = 5;
            public const int MaxNameLength = 5;
            public async Task CheckValidityAsync(CallContext callContext)
            {
                await QueryValidationHelper.CheckUserExistsAsync(callContext.DbContext, UserId);
                if (CultureName != CultureName.Trim())
                    throw new InvalidOperationException("Invalid Name: not trimmed");
                if (CultureName.Length < MinNameLength || CultureName.Length > MaxNameLength)
                    throw new InvalidOperationException($"Invalid culture name '{CultureName}'");
            }
        }
        public sealed record Result();
        #endregion
    }
}
