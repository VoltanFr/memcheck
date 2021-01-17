using MemCheck.Database;
using MemCheck.Domain;
using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Collections.Immutable;
using MemCheck.Application.QueryValidation;

namespace MemCheck.Application.Loading
{
    public sealed class GetCardForEdit
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public GetCardForEdit(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task<ResultModel> RunAsync(Request request)
        {
            await request.CheckValidityAsync(dbContext);

            var card = await dbContext.Cards
                .Include(card => card.VersionCreator)
                .Include(card => card.Images)
                .ThenInclude(img => img.Image)
                .Include(card => card.CardLanguage)
                .Include(card => card.TagsInCards)
                .ThenInclude(tagInCard => tagInCard.Tag)
                .Include(card => card.UsersWithView)
                .ThenInclude(userWithView => userWithView.User)
                .Where(card => card.Id == request.CardId)
                .AsSingleQuery()
                .SingleOrDefaultAsync();

            if (card == null)
                throw new RequestInputException("Card not found in database");

            var ratings = CardRatings.Load(dbContext, request.CurrentUserId, ImmutableHashSet.Create(request.CardId));

            var ownersOfDecksWithThisCard = dbContext.CardsInDecks
                .Where(cardInDeck => cardInDeck.CardId == request.CardId)
                .Select(cardInDeck => cardInDeck.Deck.Owner.UserName)
                .Distinct();

            return new ResultModel(
                card.FrontSide,
                card.BackSide,
                card.AdditionalInfo,
                card.CardLanguage.Id,
                card.TagsInCards.Select(tagInCard => new ResultTagModel(tagInCard.TagId, tagInCard.Tag.Name)),
                card.UsersWithView.Select(userWithView => new ResultUserModel(userWithView.UserId, userWithView.User.UserName)),
                card.InitialCreationUtcDate,
                card.VersionUtcDate,
                card.VersionCreator.UserName,
                card.VersionDescription,
                ownersOfDecksWithThisCard,
                card.Images.Select(img => new ResultImageModel(img)),
                ratings.User(request.CardId),
                ratings.Average(request.CardId),
                ratings.Count(request.CardId)
                );
        }
        #region Result classes
        public sealed class Request
        {
            public Request(Guid currentUserId, Guid cardId)
            {
                CurrentUserId = currentUserId;
                CardId = cardId;
            }
            public Guid CurrentUserId { get; }
            public Guid CardId { get; }
            public async Task CheckValidityAsync(MemCheckDbContext dbContext)
            {
                if (QueryValidationHelper.IsReservedGuid(CurrentUserId))
                    throw new RequestInputException($"Invalid user id '{CurrentUserId}'");
                if (QueryValidationHelper.IsReservedGuid(CardId))
                    throw new RequestInputException($"Invalid card id '{CardId}'");
                await CardVisibilityHelper.CheckUserIsAllowedToViewCardAsync(dbContext, CurrentUserId, CardId);
            }
        }
        public sealed class ResultModel
        {
            public ResultModel(string frontSide, string backSide, string additionalInfo, Guid languageId, IEnumerable<ResultTagModel> tags, IEnumerable<ResultUserModel> usersWithVisibility, DateTime creationUtcDate,
                DateTime lastVersionCreationUtcDate, string lastVersionCreator, string lastVersionDescription, IEnumerable<string> usersOwningDeckIncluding, IEnumerable<ResultImageModel> images, int userRating, double averageRating, int countOfUserRatings)
            {
                FrontSide = frontSide;
                BackSide = backSide;
                AdditionalInfo = additionalInfo;
                LanguageId = languageId;
                Tags = tags;
                UsersWithVisibility = usersWithVisibility;
                FirstVersionUtcDate = creationUtcDate;
                LastVersionUtcDate = lastVersionCreationUtcDate;
                LastVersionCreatorName = lastVersionCreator;
                LastVersionDescription = lastVersionDescription;
                UsersOwningDeckIncluding = usersOwningDeckIncluding;
                Images = images;
                UserRating = userRating;
                AverageRating = averageRating;
                CountOfUserRatings = countOfUserRatings;
            }
            public string FrontSide { get; }
            public string BackSide { get; }
            public string AdditionalInfo { get; }
            public Guid LanguageId { get; }
            public IEnumerable<ResultTagModel> Tags { get; }
            public IEnumerable<ResultUserModel> UsersWithVisibility { get; }
            public DateTime FirstVersionUtcDate { get; }
            public DateTime LastVersionUtcDate { get; }
            public string LastVersionCreatorName { get; }
            public string LastVersionDescription { get; }
            public IEnumerable<string> UsersOwningDeckIncluding { get; }
            public IEnumerable<ResultImageModel> Images { get; }
            public int UserRating { get; }
            public double AverageRating { get; }
            public int CountOfUserRatings { get; }
        }
        public sealed class ResultTagModel
        {
            public ResultTagModel(Guid tagId, string tagName)
            {
                TagId = tagId;
                TagName = tagName;
            }
            public Guid TagId { get; }
            public string TagName { get; }
        }
        public sealed class ResultUserModel
        {
            public ResultUserModel(Guid userId, string userName)
            {
                UserId = userId;
                UserName = userName;
            }
            public Guid UserId { get; }
            public string UserName { get; }
        }
        public sealed class ResultImageModel
        {
            public ResultImageModel(ImageInCard img)
            {
                ImageId = img.ImageId;
                Name = img.Image.Name;
                Source = img.Image.Source;
                CardSide = img.CardSide;
            }
            public Guid ImageId { get; }
            public string Name { get; }
            public string Source { get; }
            public int CardSide { get; }   //1 = front side ; 2 = back side ; 3 = AdditionalInfo
        }
        #endregion
    }
}
