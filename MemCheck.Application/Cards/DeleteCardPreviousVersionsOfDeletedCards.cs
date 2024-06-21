using MemCheck.Application.QueryValidation;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards;

//Deletes the full version history of deleted cards in the previous versions table (taking care of appropriate cascade deletion)
//We won't delete previous versions if the version date of the deletion entry is after Request.LimitDate
public sealed class DeleteCardPreviousVersionsOfDeletedCards : RequestRunner<DeleteCardPreviousVersionsOfDeletedCards.Request, DeleteCardPreviousVersionsOfDeletedCards.Result>
{
    #region Private methods
    private async Task DeleteAsync(Guid previousVersionId)
    {
        var version = await DbContext.CardPreviousVersions
            .Include(cardPreviousVer => cardPreviousVer.PreviousVersion)
            .Where(cardPreviousVer => cardPreviousVer.Id == previousVersionId)
            .SingleAsync();
        if (version.PreviousVersion != null)
            await DeleteAsync(version.PreviousVersion.Id);
        DbContext.CardPreviousVersions.Remove(version);
    }
    #endregion
    public DeleteCardPreviousVersionsOfDeletedCards(CallContext callContext) : base(callContext)
    {
    }
    protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
    {
        while (true)
        {
            var count = await DbContext.CardPreviousVersions
                            .Where(cardPreviousVer => cardPreviousVer.VersionType == CardPreviousVersionType.Deletion && cardPreviousVer.VersionUtcDate < request.LimitUtcDate)
                            .CountAsync();

            var deletionVersions = await DbContext.CardPreviousVersions
                .Include(cardPreviousVer => cardPreviousVer.PreviousVersion)
                .Where(cardPreviousVer => cardPreviousVer.VersionType == CardPreviousVersionType.Deletion && cardPreviousVer.VersionUtcDate < request.LimitUtcDate)
                .OrderBy(cardPreviousVer => cardPreviousVer.VersionUtcDate)
                .Select(cardPreviousVer => cardPreviousVer.Id)
                .Take(100)
                .ToListAsync();

            if (deletionVersions.Count == 0)
                return new ResultWithMetrologyProperties<Result>(new Result());

            foreach (var deletionVersion in deletionVersions)
                await DeleteAsync(deletionVersion);

            await DbContext.SaveChangesAsync();
        }
    }
    #region Request type
    public sealed record Request(Guid UserId, DateTime LimitUtcDate) : IRequest
    {
        public async Task CheckValidityAsync(CallContext callContext)
        {
            await QueryValidationHelper.CheckUserExistsAndIsAdminAsync(callContext.DbContext, UserId, callContext.RoleChecker);
        }
    }
    public sealed class Result
    {
    }
}
#endregion
