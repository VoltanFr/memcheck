using MemCheck.Application.QueryValidation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;
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
            var cultureId = MemCheckSupportedCultures.IdFromCulture(request.Culture)!;
            user.UILanguage = cultureId;
            DbContext.SaveChanges();
            return new ResultWithMetrologyProperties<Result>(new Result(), ("CultureId", cultureId));
        }
        #region Request & Result
        public sealed record Request(Guid UserId, CultureInfo Culture) : IRequest
        {
            public async Task CheckValidityAsync(CallContext callContext)
            {
                await QueryValidationHelper.CheckUserExistsAsync(callContext.DbContext, UserId);
                if (MemCheckSupportedCultures.IdFromCulture(Culture)==null)
                    throw new InvalidOperationException($"Unknown culture '{Culture}'");
            }
        }
        public sealed record Result();
        #endregion
    }
}
