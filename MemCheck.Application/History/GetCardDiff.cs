using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.History
{
    public sealed class GetCardDiff
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        #region Private methods
        #endregion
        public GetCardDiff(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task<Result> RunAsync(Request request)
        {
            await request.CheckValidityAsync(dbContext);
            var current = await dbContext.Cards.Where(c => c.Id == request.CurrentCardId).SingleAsync();
            var original = await dbContext.CardPreviousVersions.Where(c => c.Id == request.OriginalVersionId).SingleAsync();

            var result = new Result(current.VersionCreator.UserName, original.VersionCreator.UserName, current.VersionUtcDate, original.VersionUtcDate, current.VersionDescription, original.VersionDescription);
            if (current.FrontSide != original.FrontSide)
                result = result with { FrontSide = new(current.FrontSide, original.FrontSide) };
            if (current.BackSide != original.BackSide)
                result = result with { BackSide = new(current.BackSide, original.BackSide) };
            return result;

        }
        #region Request and result types
        public sealed record Request(Guid UserId, Guid CurrentCardId, Guid OriginalVersionId)
        {
            public async Task CheckValidityAsync(MemCheckDbContext dbContext)
            {
                if (QueryValidationHelper.IsReservedGuid(UserId))
                    throw new InvalidOperationException("Invalid user ID");
                var user = await dbContext.Users.Where(u => u.Id == UserId).SingleAsync();
                await Task.CompletedTask;
            }
        }
        public sealed record Result
        {
            //Properties are null when a field has not changed
            public Result(string currentVersionCreator, string originalVersionCreator, DateTime currentVersionUtcDate, DateTime originalVersionUtcDate, string currentVersionDescription, string originalVersionDescription)
            {
                CurrentVersionCreator = currentVersionCreator;
                OriginalVersionCreator = originalVersionCreator;
                CurrentVersionUtcDate = currentVersionUtcDate;
                OriginalVersionUtcDate = originalVersionUtcDate;
                CurrentVersionDescription = currentVersionDescription;
                OriginalVersionDescription = originalVersionDescription;
            }
            public string CurrentVersionCreator { get; }
            public string OriginalVersionCreator { get; }
            public DateTime CurrentVersionUtcDate { get; }
            public DateTime OriginalVersionUtcDate { get; }
            public string CurrentVersionDescription { get; }
            public string OriginalVersionDescription { get; }
            public (string currentLanguage, string originalLanguage)? Language { get; init; }
            public (string currentFrontSide, string originalFrontSide)? FrontSide;
            public (string currentBackSide, string originalBackSide)? BackSide;
            public (string currentAdditionalInfo, string originalAdditionalInfo)? AdditionalInfo;
            public (string currentTags, string originalTags)? Tags;
            public (string currentUsersWithView, string originalUsersWithView)? UsersWithView;
            public (string currentImagesOnFrontSide, string originalImagesOnFrontSide)? ImagesOnFrontSide;
            public (string currentImagesOnBackSide, string originalImagesOnBackSide)? ImagesOnBackSide;
            public (string currentImagesOnAdditionalSide, string originalImagesOnAdditionalSide)? ImagesOnAdditionalSide;
        }
        #endregion
    }
}
