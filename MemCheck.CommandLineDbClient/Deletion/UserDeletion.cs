using MemCheck.Application.QueryValidation;
using MemCheck.Application.Users;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.CommandLineDbClient.Deletion
{
    internal sealed class UserDeletion : ICmdLinePlugin
    {
        #region Fields
        private readonly ILogger<BrutalDeletion> logger;
        private readonly MemCheckDbContext dbContext;
        private readonly UserManager<MemCheckUser> userManager;
        private const string adminUserName = "admin";
        private const string userToDeleteName = "todelete";
        #endregion
        public UserDeletion(IServiceProvider serviceProvider)
        {
            dbContext = serviceProvider.GetRequiredService<MemCheckDbContext>();
            userManager = serviceProvider.GetRequiredService<UserManager<MemCheckUser>>();
            logger = serviceProvider.GetRequiredService<ILogger<BrutalDeletion>>();
        }
        public void DescribeForOpportunityToCancel()
        {
            logger.LogCritical($"Will delete user '{userToDeleteName}' - D A N G E R");
        }
        public async Task RunAsync()
        {
            var userToDelete = await dbContext.Users.Where(u => u.UserName == userToDeleteName).Select(u => new { u.Id, u.UserName }).SingleAsync();
            var userDeckIds = await dbContext.Decks.Where(d => d.Owner.Id == userToDelete.Id).Select(d => d.Id).ToListAsync();
            var userCardsInDecksCount = await dbContext.CardsInDecks.Where(cardInDeck => cardInDeck.Deck.Owner.Id == userToDelete.Id).CountAsync();
            var userCardRatingCount = await dbContext.UserCardRatings.Where(rating => rating.UserId == userToDelete.Id).CountAsync();
            var userSearchSubscriptionIds = await dbContext.SearchSubscriptions.Where(subscription => subscription.UserId == userToDelete.Id).Select(subscription => subscription.Id).ToListAsync();
            var userPrivateCardIds = await dbContext.Cards.Where(card => card.UsersWithView.Count() == 1 && card.UsersWithView.Any(userWithView => userWithView.UserId == userToDelete.Id)).Select(card => card.Id).ToListAsync();

            logger.LogInformation($"Deleting user '{userToDelete.UserName}', id: {userToDelete.Id}");
            logger.LogInformation($"User has {userDeckIds.Count} decks containing a total of {userCardsInDecksCount} cards");
            logger.LogInformation($"User has recorded {userCardRatingCount} ratings");
            logger.LogInformation($"User has {userSearchSubscriptionIds.Count} search subscriptions");
            logger.LogInformation($"User has {userPrivateCardIds.Count} private cards");
            logger.LogWarning("Please confirm again");
            Engine.GetConfirmationOrCancel(logger);

            var adminUserId = await dbContext.Users.Where(u => u.UserName == adminUserName).Select(u => u.Id).SingleAsync();

            var deleter = new DeleteUserAccount(dbContext.AsCallContext(new ProdRoleChecker(userManager)), userManager);
            await deleter.RunAsync(new DeleteUserAccount.Request(adminUserId, userToDelete.Id));

            logger.LogInformation($"Deletion finished");
        }
    }
}
