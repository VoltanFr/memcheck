using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards
{
    //Deletes the full version history of deleted cards in the previous versions table (taking care of appropriate cascade deletion)
    //We won't delete previous versions if the version date of the deletion entry is after Request.LimitDate
    public sealed class DeleteCardPreviousVersionsOfDeletedCards
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        private readonly IRoleChecker roleChecker;
        #endregion
        #region Private methods
        private async Task DeleteAsync(Guid previousVersionId)
        {
            var version = await dbContext.CardPreviousVersions
                .Include(cardPreviousVer => cardPreviousVer.PreviousVersion)
                .Where(cardPreviousVer => cardPreviousVer.Id == previousVersionId)
                .SingleAsync();
            if (version.PreviousVersion != null)
                await DeleteAsync(version.PreviousVersion.Id);
            dbContext.CardPreviousVersions.Remove(version);
        }
        #endregion
        public DeleteCardPreviousVersionsOfDeletedCards( MemCheckDbContext dbContext, IRoleChecker roleChecker)
        {
            this.dbContext = dbContext;
            this.roleChecker = roleChecker;
        }
        public async Task RunAsync(Request request)
        {
            await request.CheckValidityAsync(dbContext, roleChecker);

            while (true)
            {
                var count = await dbContext.CardPreviousVersions
                                .Where(cardPreviousVer => cardPreviousVer.VersionType == CardPreviousVersionType.Deletion && cardPreviousVer.VersionUtcDate < request.LimitUtcDate)
                                .CountAsync();

                var deletionVersions = await dbContext.CardPreviousVersions
                .Include(cardPreviousVer => cardPreviousVer.PreviousVersion)
                .Where(cardPreviousVer => cardPreviousVer.VersionType == CardPreviousVersionType.Deletion && cardPreviousVer.VersionUtcDate < request.LimitUtcDate)
                .OrderBy(cardPreviousVer => cardPreviousVer.VersionUtcDate)
                .Select(cardPreviousVer => cardPreviousVer.Id)
                .Take(100)
            .ToListAsync();

                if (!deletionVersions.Any())
                    return;

                foreach (var deletionVersion in deletionVersions)
                    await DeleteAsync(deletionVersion);

                await dbContext.SaveChangesAsync();
            }
        }
        #region Request type
        public sealed record Request(Guid UserId, DateTime LimitUtcDate)
        {
            public async Task CheckValidityAsync(MemCheckDbContext dbContext, IRoleChecker roleChecker)
            {
                await QueryValidationHelper.CheckUserExistsAndIsAdminAsync(dbContext, UserId, roleChecker);
            }
        }
    }
    #endregion
}
