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

namespace MemCheck.Application.Users;

public sealed class MemCheckUserManager : UserManager<MemCheckUser>
{
    #region Private stuff
    private readonly CallContext callContext;
    #endregion
    public const string DefaultDeckName = "Auto";

    public MemCheckUserManager(IUserStore<MemCheckUser> store, IOptions<IdentityOptions> optionsAccessor, IPasswordHasher<MemCheckUser> passwordHasher,
        IEnumerable<IUserValidator<MemCheckUser>> _, IEnumerable<IPasswordValidator<MemCheckUser>> passwordValidators, ILookupNormalizer keyNormalizer,
        IdentityErrorDescriber errors, IServiceProvider services, ILogger<UserManager<MemCheckUser>> logger, TelemetryClient telemetryClient, MemCheckDbContext dbContext)
        : base(store, optionsAccessor, passwordHasher, new MemCheckUserValidator().AsArray(), passwordValidators, keyNormalizer, errors, services, logger)
    {
        callContext = new CallContext(dbContext, new MemCheckTelemetryClient(telemetryClient), new FakeStringLocalizer(), new ProdRoleChecker(this));
    }
    public override Task<IdentityResult> DeleteAsync(MemCheckUser user)
    {
        //In MemCheck, a user account is never deleted, but anonymized (with class DeleteUserAccount)
        throw new InvalidOperationException("This is not meant to be called");
    }
    public override async Task<IdentityResult> CreateAsync(MemCheckUser user)
    {
        user.RegistrationUtcDate = DateTime.UtcNow;
        var result = await base.CreateAsync(user);
        callContext.TelemetryClient.TrackEvent("UserAccountCreated", ("UserName", user.UserName), ("Email", user.Email), ("Success", result.Succeeded.ToString()), ("ErrorList", string.Concat(result.Errors.Select(error => error.Code + ": " + error.Description))));
        if (result.Succeeded)
            await new CreateDeck(callContext).RunAsync(new CreateDeck.Request(user.Id, DefaultDeckName, HeapingAlgorithms.DefaultAlgoId));
        return result;
    }
    public override async Task<IdentityResult> ConfirmEmailAsync(MemCheckUser user, string token)
    {
        var result = await base.ConfirmEmailAsync(user, token);
        callContext.TelemetryClient.TrackEvent("UserAccountConfirmed", ("UserName", user.UserName), ("Email", user.Email), ("Success", result.Succeeded.ToString()), ("ErrorList", string.Concat(result.Errors.Select(error => error.Code + ": " + error.Description))));
        return result;
    }
    public static void SetupIdentityOptions(IdentityOptions options)
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.User.RequireUniqueEmail = false;
        options.Lockout.AllowedForNewUsers = false;
    }
}
