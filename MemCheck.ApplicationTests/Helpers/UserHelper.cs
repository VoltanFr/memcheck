using MemCheck.Application.Users;
using MemCheck.Basics;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Helpers;

public sealed class UserHelper
{
    public static UserManager<MemCheckUser> GetUserManager(MemCheckDbContext dbContext)
    {
#pragma warning disable CA2000 // Dispose objects before losing scope
        var userStore = new UserStore<MemCheckUser, MemCheckUserRole, MemCheckDbContext, Guid>(dbContext);
        var identityOptions = new IdentityOptions();
        MemCheckUserManager.SetupIdentityOptions(identityOptions);
        var optionsAccessor = Options.Create(identityOptions);
        var passwordHasher = new PasswordHasher<MemCheckUser>();
        var userValidators = new UserValidator<MemCheckUser>().AsArray();
        var passwordValidators = new PasswordValidator<MemCheckUser>().AsArray();
        var keyNormalizer = new UpperInvariantLookupNormalizer();
        var errors = new IdentityErrorDescriber();
        var services = new ServiceCollection();
        var logger = new LoggerFactory().CreateLogger<UserManager<MemCheckUser>>();
        var serviceProvider = services.BuildServiceProvider();
        var telemetryClient = new TelemetryClient(new TelemetryConfiguration());
        return new MemCheckUserManager(userStore, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, serviceProvider, logger, telemetryClient, dbContext);
#pragma warning restore CA2000 // Dispose objects before losing scope
    }
    public static async Task<Guid> CreateInDbAsync(DbContextOptions<MemCheckDbContext> db, int minimumCountOfDaysBetweenNotifs = 0, DateTime? lastNotificationUtcDate = null, bool subscribeToCardOnEdit = false, string? userName = null, string? userEMail = null)
    {
        using var dbContext = new MemCheckDbContext(db);
        var result = Create(minimumCountOfDaysBetweenNotifs, lastNotificationUtcDate, subscribeToCardOnEdit, userName);
        if (userEMail != null)
        {
            result.Email = userEMail;
            result.EmailConfirmed = true;
        }
        dbContext.Users.Add(result);
        await dbContext.SaveChangesAsync();
        return result.Id;
    }
    public static MemCheckUser Create(int minimumCountOfDaysBetweenNotifs = 0, DateTime? lastNotificationUtcDate = null, bool subscribeToCardOnEdit = false, string? userName = null, string? email = null)
    {
        return new MemCheckUser
        {
            MinimumCountOfDaysBetweenNotifs = minimumCountOfDaysBetweenNotifs,
            LastNotificationUtcDate = lastNotificationUtcDate ?? DateTime.MinValue,
            SubscribeToCardOnEdit = subscribeToCardOnEdit,
            UserName = userName ?? RandomHelper.String(firstCharMustBeLetter: true),
            Email = email ?? RandomHelper.Email(),
            EmailConfirmed = true,
        };
    }
    public static async Task SetRandomPasswordAsync(DbContextOptions<MemCheckDbContext> db, Guid userId)
    {
        using var dbContext = new MemCheckDbContext(db);
        var userToDelete = dbContext.Users.Single(u => u.Id == userId);
        using var userManager = GetUserManager(dbContext);
        var addPasswordResult = await userManager.AddPasswordAsync(userToDelete, RandomHelper.String().ToUpperInvariant() + RandomHelper.String());
        Assert.IsTrue(addPasswordResult.Succeeded);
    }
    public static async Task DeleteAsync(DbContextOptions<MemCheckDbContext> db, Guid userToDeleteId, Guid? deleterUserId = null)
    {
        DeleteUserAccount.Request request;
        TestRoleChecker roleChecker;
        if (deleterUserId == null)
        {
            roleChecker = new TestRoleChecker();
            request = new DeleteUserAccount.Request(userToDeleteId, userToDeleteId);
        }
        else
        {
            roleChecker = new TestRoleChecker(deleterUserId.Value);
            request = new DeleteUserAccount.Request(deleterUserId.Value, userToDeleteId);
        }

        using var dbContext = new MemCheckDbContext(db);
        using var userManager = GetUserManager(dbContext);
        await new DeleteUserAccount(dbContext.AsCallContext(roleChecker), userManager).RunAsync(request);
    }
}
