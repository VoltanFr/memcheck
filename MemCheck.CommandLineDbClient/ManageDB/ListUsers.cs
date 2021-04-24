using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.CommandLineDbClient.ManageDB
{
    internal sealed class ListUsers : IMemCheckTest
    {
        #region Fields
        private readonly ILogger<ListUsers> logger;
        private readonly MemCheckDbContext dbContext;
        private readonly UserManager<MemCheckUser> userManager;
        #endregion
        public ListUsers(IServiceProvider serviceProvider)
        {
            dbContext = serviceProvider.GetRequiredService<MemCheckDbContext>();
            logger = serviceProvider.GetRequiredService<ILogger<ListUsers>>();
            userManager = serviceProvider.GetRequiredService<UserManager<MemCheckUser>>();
        }
        public void DescribeForOpportunityToCancel()
        {
            logger.LogInformation("Will list users");
        }
        async public Task RunAsync()
        {
            var users = await dbContext.Users.Select(u => new { u.Id, u.UserName }).ToListAsync();

            foreach (var user in users)
                logger.LogInformation($"User '{user.UserName}' has id {user.Id}");
        }
    }
}
