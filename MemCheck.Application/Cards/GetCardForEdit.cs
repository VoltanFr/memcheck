using MemCheck.Application.QueryValidation;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards;

public sealed class GetCardForEdit : RequestRunner<GetCardForEdit.Request, GetCardForEdit.ResultModel>
{
    public GetCardForEdit(CallContext callContext) : base(callContext)
    {
    }
    protected override async Task<ResultWithMetrologyProperties<ResultModel>> DoRunAsync(Request request)
    {
        var card = await DbContext.Cards
            .AsNoTracking()
            .Include(card => card.VersionCreator)
            .Include(card => card.CardLanguage)
            .Include(card => card.TagsInCards)
            .ThenInclude(tagInCard => tagInCard.Tag)
            .Include(card => card.UsersWithView)
            .ThenInclude(userWithView => userWithView.User)
            .Where(card => card.Id == request.CardId)
            .AsSingleQuery()
            .SingleAsync();

        var userRating = await DbContext.UserCardRatings.SingleOrDefaultAsync(c => c.CardId == card.Id && c.UserId == request.CurrentUserId);
        var userRatingValue = userRating == null ? 0 : userRating.Rating;

        var ownersOfDecksWithThisCard = DbContext.CardsInDecks
            .AsNoTracking()
            .Where(cardInDeck => cardInDeck.CardId == request.CardId)
            .Select(cardInDeck => cardInDeck.Deck.Owner.GetUserName())
            .Distinct();

        var possibleTargetDecksForAdd = DbContext.Decks
            .AsNoTracking()
            .Where(deck => deck.Owner.Id == request.CurrentUserId && !deck.CardInDecks.Any(card => card.CardId == request.CardId))
            .Select(deck => new ResultDeckModel(deck.Id, deck.Description))
            .ToImmutableArray();

        var result = new ResultModel(
                        card.FrontSide,
                        card.BackSide,
                        card.AdditionalInfo,
                        card.References,
                        card.CardLanguage.Id,
                        card.CardLanguage.Name,
                        card.TagsInCards.Select(tagInCard => new ResultTagModel(tagInCard.TagId, tagInCard.Tag.Name)),
                        card.UsersWithView.Select(userWithView => new ResultUserModel(userWithView.UserId, userWithView.User.GetUserName())),
                        card.InitialCreationUtcDate,
                        card.VersionUtcDate,
                        card.VersionCreator.GetUserName(),
                        card.VersionDescription,
                        ownersOfDecksWithThisCard,
                        userRatingValue,
                        card.AverageRating,
                        card.RatingCount,
                        possibleTargetDecksForAdd
                        );

        return new ResultWithMetrologyProperties<ResultModel>(result,
            ("CardId", request.CardId.ToString()),
            ("CardIsPublic", CardVisibilityHelper.CardIsPublic(card.UsersWithView).ToString()),
            ("CardIsPrivateToSingleUser", CardVisibilityHelper.CardIsPrivateToSingleUser(request.CurrentUserId, card.UsersWithView).ToString()),
            DoubleMetric("CardAverageRating", card.AverageRating),
            IntMetric("CardUserRating", userRatingValue),
            DoubleMetric("AgeOfCurrentVersionInDays", (DateTime.UtcNow - card.VersionUtcDate).TotalDays),
            DoubleMetric("AgeOfCardInDays", (DateTime.UtcNow - card.InitialCreationUtcDate).TotalDays),
            ("CardLanguage", card.CardLanguage.Name));
    }
    #region Request & Result classes
    public sealed class Request : IRequest
    {
        public Request(Guid currentUserId, Guid cardId)
        {
            CurrentUserId = currentUserId;
            CardId = cardId;
        }
        public Guid CurrentUserId { get; }
        public Guid CardId { get; }
        public async Task CheckValidityAsync(CallContext callContext)
        {
            QueryValidationHelper.CheckNotReservedGuid(CurrentUserId);
            QueryValidationHelper.CheckNotReservedGuid(CardId);
            await QueryValidationHelper.CheckCardExistsAsync(callContext.DbContext, CardId);
            CardVisibilityHelper.CheckUserIsAllowedToViewCard(callContext.DbContext, CurrentUserId, CardId);
        }
    }
    public sealed class ResultModel
    {
        public ResultModel(string frontSide, string backSide, string additionalInfo, string references, Guid languageId, string languageName, IEnumerable<ResultTagModel> tags, IEnumerable<ResultUserModel> usersWithVisibility, DateTime creationUtcDate,
            DateTime lastVersionCreationUtcDate, string lastVersionCreator, string lastVersionDescription, IEnumerable<string> usersOwningDeckIncluding, int userRating, double averageRating, int countOfUserRatings, ImmutableArray<ResultDeckModel> possibleTargetDecksForAdd)
        {
            FrontSide = frontSide;
            BackSide = backSide;
            AdditionalInfo = additionalInfo;
            References = references;
            LanguageId = languageId;
            LanguageName = languageName;
            Tags = tags;
            UsersWithVisibility = usersWithVisibility;
            FirstVersionUtcDate = creationUtcDate;
            LastVersionUtcDate = lastVersionCreationUtcDate;
            LastVersionCreatorName = lastVersionCreator;
            LastVersionDescription = lastVersionDescription;
            UsersOwningDeckIncluding = usersOwningDeckIncluding;
            UserRating = userRating;
            AverageRating = averageRating;
            CountOfUserRatings = countOfUserRatings;
            PossibleTargetDecksForAdd = possibleTargetDecksForAdd;
        }
        public string FrontSide { get; }
        public string BackSide { get; }
        public string AdditionalInfo { get; }
        public string References { get; }
        public Guid LanguageId { get; }
        public string LanguageName { get; }
        public IEnumerable<ResultTagModel> Tags { get; }
        public IEnumerable<ResultUserModel> UsersWithVisibility { get; }
        public DateTime FirstVersionUtcDate { get; }
        public DateTime LastVersionUtcDate { get; }
        public string LastVersionCreatorName { get; }
        public string LastVersionDescription { get; }
        public IEnumerable<string> UsersOwningDeckIncluding { get; }
        public int UserRating { get; }
        public double AverageRating { get; }
        public int CountOfUserRatings { get; }
        public ImmutableArray<ResultDeckModel> PossibleTargetDecksForAdd { get; }
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
    public sealed class ResultDeckModel
    {
        public ResultDeckModel(Guid deckId, string deckName)
        {
            DeckId = deckId;
            DeckName = deckName;
        }
        public Guid DeckId { get; }
        public string DeckName { get; }
    }
    #endregion
}
