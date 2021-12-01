using MemCheck.Application.QueryValidation;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace MemCheck.Application.Languages
{
    public sealed class CreateLanguage : RequestRunner<CreateLanguage.Request, CreateLanguage.Result>
    {
        public CreateLanguage(CallContext callContext) : base(callContext)
        {
        }
        protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
        {
            var language = new CardLanguage() { Name = request.Name };
            DbContext.CardLanguages.Add(language);
            await DbContext.SaveChangesAsync();
            var result = new Result(language.Id, language.Name, 0);
            return new ResultWithMetrologyProperties<Result>(result, ("Name", request.Name));
        }
        #region Request & Result
        public sealed record Request(Guid UserId, string Name) : IRequest
        {
            public async Task CheckValidityAsync(CallContext callContext)
            {
                await QueryValidationHelper.CheckCanCreateLanguageWithName(Name, callContext.DbContext, callContext.Localized);
                await QueryValidationHelper.CheckUserExistsAsync(callContext.DbContext, UserId);
                var user = await callContext.DbContext.Users.AsNoTracking().SingleAsync(user => user.Id == UserId);
                if (!await callContext.RoleChecker.UserIsAdminAsync(user))
                    throw new InvalidOperationException("User not admin");
            }
        }
        public sealed record Result(Guid Id, string Name, int CardCount);
        #endregion
    }
}
