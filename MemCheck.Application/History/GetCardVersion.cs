using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.History
{
    public sealed class GetCardVersion
    {
        #region Fields
        private readonly CallContext callContext;
        #endregion
        public GetCardVersion(CallContext callContext)
        {
            this.callContext = callContext;
        }
        public async Task<Result> RunAsync(Request request)
        {
            await request.CheckValidityAsync(callContext.DbContext);

            var version = await callContext.DbContext.CardPreviousVersions
                .Include(card => card.Images)
                .ThenInclude(img => img.Image)
                .Include(card => card.CardLanguage)
                .Include(card => card.Tags)
                .ThenInclude(tagInCard => tagInCard.Tag)
                .Include(card => card.UsersWithView)
                .Where(card => card.Id == request.VersionId)
                .AsSingleQuery()
                .SingleOrDefaultAsync();

            var userWithViewNames = version.UsersWithView.Select(userWithView => callContext.DbContext.Users.Single(u => u.Id == userWithView.AllowedUserId).UserName);
            var tagNames = version.Tags.Select(t => t.Tag.Name);
            var frontSideImageNames = version.Images.Where(i => i.CardSide == ImageInCard.FrontSide).Select(i => i.Image.Name);
            var backSideImageNames = version.Images.Where(i => i.CardSide == ImageInCard.BackSide).Select(i => i.Image.Name);
            var additionalInfoImageNames = version.Images.Where(i => i.CardSide == ImageInCard.AdditionalInfo).Select(i => i.Image.Name);

            callContext.TelemetryClient.TrackEvent("GetCardVersion", ("VersionId", request.VersionId.ToString()));

            return new Result(
                version.FrontSide,
                version.BackSide,
                version.AdditionalInfo,
                version.CardLanguage.Id,
                version.CardLanguage.Name,
                tagNames,
                userWithViewNames,
                version.VersionUtcDate,
                frontSideImageNames,
                backSideImageNames,
                additionalInfoImageNames,
                version.VersionDescription,
                version.VersionCreator.UserName
                );
        }
        #region Request & Result types
        public sealed record Request(Guid UserId, Guid VersionId)
        {
            public async Task CheckValidityAsync(MemCheckDbContext dbContext)
            {
                if (QueryValidationHelper.IsReservedGuid(UserId))
                    throw new InvalidOperationException("Invalid user ID");
                var user = await dbContext.Users.SingleAsync(u => u.Id == UserId);

                var cardVersion = await dbContext.CardPreviousVersions.Include(v => v.UsersWithView).SingleAsync(v => v.Id == VersionId);
                if (!CardVisibilityHelper.CardIsVisibleToUser(UserId, cardVersion.UsersWithView.Select(uwv => uwv.AllowedUserId)))
                    throw new InvalidOperationException("Original not visible to user");
            }
        }
        public sealed record Result(string FrontSide, string BackSide, string AdditionalInfo, Guid LanguageId, string LanguageName, IEnumerable<string> Tags, IEnumerable<string> UsersWithVisibility, DateTime VersionUtcDate,
                IEnumerable<string> FrontSideImageNames, IEnumerable<string> BackSideImageNames, IEnumerable<string> AdditionalInfoImageNames, string VersionDescription, string CreatorName);
        #endregion
    }

}
