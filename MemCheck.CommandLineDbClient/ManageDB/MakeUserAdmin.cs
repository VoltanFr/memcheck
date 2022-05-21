using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.CommandLineDbClient.ManageDB;

internal sealed class MakeUserAdmin : ICmdLinePlugin
{
    #region Fields
    private readonly ILogger<MakeUserAdmin> logger;
    private readonly MemCheckDbContext dbContext;
    private readonly RoleManager<MemCheckUserRole> roleManager;
    private readonly UserManager<MemCheckUser> userManager;
    private readonly ListUsers listUsers;
    private const string userName = "TargetUserName";
    #endregion
    public MakeUserAdmin(IServiceProvider serviceProvider)
    {
        dbContext = serviceProvider.GetRequiredService<MemCheckDbContext>();
        logger = serviceProvider.GetRequiredService<ILogger<MakeUserAdmin>>();
        roleManager = serviceProvider.GetRequiredService<RoleManager<MemCheckUserRole>>();
        userManager = serviceProvider.GetRequiredService<UserManager<MemCheckUser>>();
        listUsers = new ListUsers(serviceProvider);
    }
    public void DescribeForOpportunityToCancel()
    {
        logger.LogInformation($"Will update account of user '{userName}' as email-confirmed and admin");
    }
    public async Task RunAsync()
    {
        var user = await dbContext.Users.Where(user => user.UserName == userName).SingleOrDefaultAsync();
        if (user == null)
        {
            await listUsers.RunAsync();
            throw new InvalidProgramException($"User '{userName}' not found in database");
        }
        user.EmailConfirmed = true;
        await dbContext.SaveChangesAsync();

        if (!await roleManager.RoleExistsAsync(IRoleChecker.AdminRoleName))
        {
            var adminRole = new MemCheckUserRole() { Name = IRoleChecker.AdminRoleName };
            await roleManager.CreateAsync(adminRole);
        }

        await userManager.AddToRoleAsync(user, IRoleChecker.AdminRoleName);
    }
}
