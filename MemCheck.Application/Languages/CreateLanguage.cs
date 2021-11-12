using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace MemCheck.Application.Languages
{
    public sealed class CreateLanguage
    {
        #region Fields
        private readonly CallContext callContext;
        private readonly IRoleChecker roleChecker;
        #endregion
        public CreateLanguage(CallContext callContext, IRoleChecker roleChecker)
        {
            this.callContext = callContext;
            this.roleChecker = roleChecker;
        }
        public async Task<Result> RunAsync(Request request, ILocalized localizer)
        {
            await request.CheckValidityAsync(localizer, callContext.DbContext, roleChecker);
            var language = new CardLanguage() { Name = request.Name };
            callContext.DbContext.CardLanguages.Add(language);
            await callContext.DbContext.SaveChangesAsync();
            var result = new Result(language.Id, language.Name, 0);
            callContext.TelemetryClient.TrackEvent("CreateLanguage", ("Name", request.Name));
            return result;
        }
        #region Request type
        public sealed record Request(Guid UserId, string Name)
        {
            public async Task CheckValidityAsync(ILocalized localizer, MemCheckDbContext dbContext, IRoleChecker roleChecker)
            {
                await QueryValidationHelper.CheckCanCreateLanguageWithName(Name, dbContext, localizer);
                await QueryValidationHelper.CheckUserExistsAsync(dbContext, UserId);
                var user = await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == UserId);
                if (!await roleChecker.UserIsAdminAsync(user))
                    throw new InvalidOperationException("User not admin");
            }
        }
        public sealed record Result(Guid Id, string Name, int CardCount);
        #endregion
    }
}
