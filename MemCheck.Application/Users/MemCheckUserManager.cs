using MemCheck.Application.Decks;
using MemCheck.Application.Heaping;
using MemCheck.Application.QueryValidation;
using MemCheck.Basics;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Users
{
    public sealed class MemCheckUserManager : UserManager<MemCheckUser>
    {
        #region Private stuff
        private sealed class FakeStringLocalizer : ILocalized
        {
            public string Get(string resourceName)
            {
                return "no translation";
            }
        }
        private readonly CallContext callContext;
        #endregion

        public MemCheckUserManager(IUserStore<MemCheckUser> store, IOptions<IdentityOptions> optionsAccessor, IPasswordHasher<MemCheckUser> passwordHasher,
            IEnumerable<IUserValidator<MemCheckUser>> userValidators, IEnumerable<IPasswordValidator<MemCheckUser>> passwordValidators, ILookupNormalizer keyNormalizer,
            IdentityErrorDescriber errors, IServiceProvider services, ILogger<UserManager<MemCheckUser>> logger, TelemetryClient telemetryClient, MemCheckDbContext dbContext)
            : base(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
        {
            callContext = new CallContext(dbContext, new MemCheckTelemetryClient(telemetryClient), new FakeStringLocalizer());
        }
        public override Task<IdentityResult> DeleteAsync(MemCheckUser user)
        {
            //In MemCheck, a user account is never deleted, but anonymized (with class DeleteUserAccount)
            throw new NotImplementedException("This is not meant to be called");
        }
        public async override Task<IdentityResult> CreateAsync(MemCheckUser user)
        {
            var result = await base.CreateAsync(user);
            callContext.TelemetryClient.TrackEvent("UserAccountCreated", ("UserName", user.UserName), ("Email", user.Email), ("Success", result.Succeeded.ToString()), ("ErrorList", string.Concat(result.Errors.Select(error => error.Code + ": " + error.Description))));
            return result;
        }
        public async override Task<IdentityResult> ConfirmEmailAsync(MemCheckUser user, string token)
        {
            var result = await base.ConfirmEmailAsync(user, token);
            callContext.TelemetryClient.TrackEvent("UserAccountConfirmed", ("UserName", user.UserName), ("Email", user.Email), ("Success", result.Succeeded.ToString()), ("ErrorList", string.Concat(result.Errors.Select(error => error.Code + ": " + error.Description))));
            return result;
        }
    }
}
