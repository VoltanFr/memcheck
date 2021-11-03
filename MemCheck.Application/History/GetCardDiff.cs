using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.History
{
    public sealed class GetCardDiff
    {
        #region Fields
        private readonly CallContext callContext;
        #endregion
        #region Private methods
        #endregion
        public GetCardDiff(CallContext callContext)
        {
            this.callContext = callContext;
        }
        public async Task<Result> RunAsync(Request request)
        {
            await request.CheckValidityAsync(callContext.DbContext);
            var current = await callContext.DbContext.Cards
                .Include(c => c.CardLanguage)
                .Include(c => c.TagsInCards)
                .ThenInclude(t => t.Tag)
                .Include(c => c.Images)
                .ThenInclude(i => i.Image)
                .SingleAsync(c => c.Id == request.CurrentCardId);
            var original = await callContext.DbContext.CardPreviousVersions
                .Include(c => c.CardLanguage)
                .Include(c => c.Tags)
                .ThenInclude(t => t.Tag)
                .Include(c => c.Images)
                .ThenInclude(i => i.Image)
                .SingleAsync(c => c.Id == request.OriginalVersionId);

            var result = new Result(current.VersionCreator.UserName, original.VersionCreator.UserName, current.VersionUtcDate, original.VersionUtcDate, current.VersionDescription, original.VersionDescription);
            if (current.FrontSide != original.FrontSide)
                result = result with { FrontSide = new(current.FrontSide, original.FrontSide) };
            if (current.BackSide != original.BackSide)
                result = result with { BackSide = new(current.BackSide, original.BackSide) };
            if (current.AdditionalInfo != original.AdditionalInfo)
                result = result with { AdditionalInfo = new(current.AdditionalInfo, original.AdditionalInfo) };
            if (current.CardLanguage != original.CardLanguage)
                result = result with { Language = new(current.CardLanguage.Name, original.CardLanguage.Name) };
            if (!Enumerable.SequenceEqual(current.TagsInCards.Select(t => t.Tag.Name).OrderBy(tagName => tagName), original.Tags.Select(t => t.Tag.Name).OrderBy(tagName => tagName)))
            {
                var currentTags = string.Join(",", current.TagsInCards.Select(t => t.Tag.Name).OrderBy(tagName => tagName));
                var originalTags = string.Join(",", original.Tags.Select(t => t.Tag.Name).OrderBy(tagName => tagName));
                result = result with { Tags = new(currentTags, originalTags) };
            }
            if (!CardVisibilityHelper.CardsHaveSameUsersWithView(current.UsersWithView, original.UsersWithView))
            {
                var currentUsers = string.Join(",", current.UsersWithView.Select(u => u.User.UserName).OrderBy(userName => userName));
                var originalUserIds = original.UsersWithView.Select(u => u.AllowedUserId).ToHashSet();
                var originalUserNames = callContext.DbContext.Users.Where(u => originalUserIds.Contains(u.Id)).Select(u => u.UserName);
                var originalUsers = string.Join(",", originalUserNames.OrderBy(userName => userName));
                result = result with { UsersWithView = new(currentUsers, originalUsers) };
            }
            if (!ComparisonHelper.SameSetOfGuid(current.Images.Where(i => i.CardSide == ImageInCard.FrontSide).Select(i => i.ImageId), original.Images.Where(i => i.CardSide == ImageInCard.FrontSide).Select(i => i.ImageId)))
            {
                var currentImages = string.Join(",", current.Images.Where(i => i.CardSide == ImageInCard.FrontSide).Select(i => i.Image.Name).OrderBy(imageName => imageName));
                var originalImages = string.Join(",", original.Images.Where(i => i.CardSide == ImageInCard.FrontSide).Select(i => i.Image.Name).OrderBy(imageName => imageName));
                result = result with { ImagesOnFrontSide = new(currentImages, originalImages) };
            }
            if (!ComparisonHelper.SameSetOfGuid(current.Images.Where(i => i.CardSide == ImageInCard.BackSide).Select(i => i.ImageId), original.Images.Where(i => i.CardSide == ImageInCard.BackSide).Select(i => i.ImageId)))
            {
                var currentImages = string.Join(",", current.Images.Where(i => i.CardSide == ImageInCard.BackSide).Select(i => i.Image.Name).OrderBy(imageName => imageName));
                var originalImages = string.Join(",", original.Images.Where(i => i.CardSide == ImageInCard.BackSide).Select(i => i.Image.Name).OrderBy(imageName => imageName));
                result = result with { ImagesOnBackSide = new(currentImages, originalImages) };
            }
            if (!ComparisonHelper.SameSetOfGuid(current.Images.Where(i => i.CardSide == ImageInCard.AdditionalInfo).Select(i => i.ImageId), original.Images.Where(i => i.CardSide == ImageInCard.AdditionalInfo).Select(i => i.ImageId)))
            {
                var currentImages = string.Join(",", current.Images.Where(i => i.CardSide == ImageInCard.AdditionalInfo).Select(i => i.Image.Name).OrderBy(imageName => imageName));
                var originalImages = string.Join(",", original.Images.Where(i => i.CardSide == ImageInCard.AdditionalInfo).Select(i => i.Image.Name).OrderBy(imageName => imageName));
                result = result with { ImagesOnAdditionalSide = new(currentImages, originalImages) };
            }
            callContext.TelemetryClient.TrackEvent("GetCardDiff", ("CurrentCardId", request.CurrentCardId.ToString()), ("OriginalVersionId", request.OriginalVersionId.ToString()));
            return result;
        }
        #region Request and result types
        public sealed record Request(Guid UserId, Guid CurrentCardId, Guid OriginalVersionId)
        {
            public async Task CheckValidityAsync(MemCheckDbContext dbContext)
            {
                if (QueryValidationHelper.IsReservedGuid(UserId))
                    throw new InvalidOperationException("Invalid user ID");
                var user = await dbContext.Users.SingleAsync(u => u.Id == UserId);

                var currentCard = await dbContext.Cards.Include(v => v.UsersWithView).SingleAsync(v => v.Id == CurrentCardId);
                if (!CardVisibilityHelper.CardIsVisibleToUser(UserId, currentCard.UsersWithView))
                    throw new InvalidOperationException("Current not visible to user");

                var originalCard = await dbContext.CardPreviousVersions.Include(v => v.UsersWithView).SingleAsync(v => v.Id == OriginalVersionId);
                if (!CardVisibilityHelper.CardIsVisibleToUser(UserId, originalCard.UsersWithView.Select(uwv => uwv.AllowedUserId)))
                    throw new InvalidOperationException("Original not visible to user");
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
