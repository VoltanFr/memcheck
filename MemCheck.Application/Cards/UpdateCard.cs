using MemCheck.Application.Images;
using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards;

public sealed class UpdateCard : RequestRunner<UpdateCard.Request, UpdateCard.Result>
{
    #region Field
    private readonly DateTime? cardNewVersionUtcDate;
    #endregion
    #region Private methods
    private async Task UpdateUsersWithViewAsync(Request request, Card card)
    {
        //We could do better than the current implem: just add the needed users and remove the unwanted

        var usersWithVisibilityBeforeUpdate = DbContext.UsersWithViewOnCards.Where(userWithView => userWithView.CardId == request.CardId);
        DbContext.UsersWithViewOnCards.RemoveRange(usersWithVisibilityBeforeUpdate);

        var usersWithView = new List<UserWithViewOnCard>();
        foreach (var userFromRequestId in request.UsersWithVisibility)
        {
            var userFromDb = DbContext.Users.Where(u => u.Id == userFromRequestId).Single();
            var userWithView = new UserWithViewOnCard() { UserId = userFromDb.Id, User = userFromDb, CardId = card.Id, Card = card };
            await DbContext.UsersWithViewOnCards.AddAsync(userWithView);
            usersWithView.Add(userWithView);
        }

        card.UsersWithView = usersWithView;
    }
    private async Task UpdateTagsAsync(Request request, Card card)
    {
        var tagsBeforeUpdate = DbContext.TagsInCards.Where(tag => tag.CardId == request.CardId);
        DbContext.TagsInCards.RemoveRange(tagsBeforeUpdate);

        var tagsInCards = new List<TagInCard>();
        foreach (var tagToAdd in request.Tags)
        {
            var tagFromDb = DbContext.Tags.Where(t => t.Id == tagToAdd).Single();
            var tagInCard = new TagInCard() { TagId = tagFromDb.Id, Tag = tagFromDb, CardId = card.Id };
            await DbContext.TagsInCards.AddAsync(tagInCard);
            tagsInCards.Add(tagInCard);
        }
        card.TagsInCards = tagsInCards;
    }
    #endregion
    public UpdateCard(CallContext callContext, DateTime? cardNewVersionUtcDate = null) : base(callContext)
    {
        this.cardNewVersionUtcDate = cardNewVersionUtcDate;
    }
    protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
    {
        var previousVersionCreator = new PreviousCardVersionCreator(DbContext);
        var card = await previousVersionCreator.RunAsync(request.CardId, request.VersionCreatorId, request.VersionDescription, cardNewVersionUtcDate);

        card.CardLanguage = DbContext.CardLanguages.Where(language => language.Id == request.LanguageId).Single();
        card.FrontSide = request.FrontSide;
        card.BackSide = request.BackSide;
        card.AdditionalInfo = request.AdditionalInfo;
        card.References = request.References;
        await UpdateTagsAsync(request, card);
        await UpdateUsersWithViewAsync(request, card);

        var imageInCardNames = ImageLoadingHelper.GetMnesiosImagesFromSides(card.FrontSide, card.BackSide, card.AdditionalInfo);
        var imageInCardIds = new HashSet<Guid>();
        foreach (var imageName in imageInCardNames)
        {
            var image = await DbContext.Images.AsNoTracking().Select(image => new { image.Id, image.Name }).SingleOrDefaultAsync(image => EF.Functions.Like(image.Name, $"{imageName}"));
            if (image != null)
            {
                if (!await DbContext.ImagesInCards.AnyAsync(imageInCard => imageInCard.ImageId == image.Id && imageInCard.CardId == card.Id))
                    DbContext.ImagesInCards.Add(new ImageInCard() { CardId = card.Id, ImageId = image.Id });
                imageInCardIds.Add(image.Id);
            }
        }
        var imagesInCardsToDelete = DbContext.ImagesInCards.Where(imageInCard => !imageInCardIds.Contains(imageInCard.ImageId) && imageInCard.CardId == card.Id);
        DbContext.ImagesInCards.RemoveRange(imagesInCardsToDelete);

        await DbContext.SaveChangesAsync();

        return new ResultWithMetrologyProperties<Result>(new Result(), ("CardId", request.CardId.ToString()));
    }
    #region Request and Result
    public sealed record Request : ICardInput
    {
        #region Private methods
        private void CheckAtLeastOneFieldDifferent(Card card, ILocalized localizer)
        {
            var dataBeforeUpdate = new
            {
                CardLanguageId = card.CardLanguage.Id,
                card.FrontSide,
                card.BackSide,
                card.AdditionalInfo,
                card.References,
                TagIds = card.TagsInCards.Select(tag => tag.TagId),
                UserWithVisibilityIds = card.UsersWithView.Select(u => u.UserId)
            };

            if ((dataBeforeUpdate.CardLanguageId == LanguageId)
                && (dataBeforeUpdate.FrontSide == FrontSide)
                && (dataBeforeUpdate.BackSide == BackSide)
                && (dataBeforeUpdate.AdditionalInfo == AdditionalInfo)
                && (dataBeforeUpdate.References == References)
                && Enumerable.SequenceEqual(dataBeforeUpdate.TagIds.OrderBy(tagId => tagId), Tags.OrderBy(tagId => tagId))
                && Enumerable.SequenceEqual(dataBeforeUpdate.UserWithVisibilityIds.OrderBy(userId => userId), UsersWithVisibility.OrderBy(userId => userId)))
                throw new RequestInputException(localizer.GetLocalized("CanNotUpdateBecauseNoDifference"));
        }
        private async Task<IEnumerable<Guid>> GetAllAuthorsAsync(Card card, MemCheckDbContext dbContext)
        {
            var result = await dbContext.CardPreviousVersions.Where(previousVersion => previousVersion.Card == CardId).Select(previousVersion => previousVersion.VersionCreator.Id).ToListAsync();
            result.Add(card.VersionCreator.Id);
            return result;
        }
        private async Task<IEnumerable<Guid>> GetAllUsersWithCardInADeckAsync(MemCheckDbContext dbContext)
        {
            return await dbContext.Decks.Join(
                dbContext.CardsInDecks.Where(cardInDeck => cardInDeck.CardId == CardId),
                deck => deck.Id,
                cardInDeck => cardInDeck.DeckId,
                (deck, cardInDeck) => deck.Owner.Id)
                .ToListAsync();
        }
        private async Task CheckVisibilityForUsersAsync(IEnumerable<Guid> userIds, ILocalized localizer, string messageSuffixId, MemCheckDbContext dbContext)
        {
            foreach (var userId in userIds)
                if (!UsersWithVisibility.Any(u => u == userId))
                {
                    var user = await dbContext.Users.SingleAsync(u => u.Id == userId);
                    throw new RequestInputException($"{localizer.GetLocalized("User")} {user.UserName} {localizer.GetLocalized("MustHaveVisibilityBecause")} {localizer.GetLocalized(messageSuffixId)}");
                }
        }
        private async Task CheckNewVisibilityAsync(Card card, ILocalized localizer, MemCheckDbContext dbContext)
        {
            if (!UsersWithVisibility.Any())
                return;
            await CheckVisibilityForUsersAsync(await GetAllAuthorsAsync(card, dbContext), localizer, "HeCreatedAVersionOfThisCard", dbContext);
            var userIds = await GetAllUsersWithCardInADeckAsync(dbContext);
            await CheckVisibilityForUsersAsync(userIds, localizer, "HeHasThisCardInADeck", dbContext);
        }
        #endregion
        public Request(Guid cardId, Guid versionCreatorId, string frontSide, string backSide, string additionalInfo, string references, Guid languageId, IEnumerable<Guid> tags, IEnumerable<Guid> usersWithVisibility, string versionDescription)
        {
            CardId = cardId;
            VersionCreatorId = versionCreatorId;
            FrontSide = frontSide.Trim();
            BackSide = backSide.Trim();
            AdditionalInfo = additionalInfo.Trim();
            References = references.Trim();
            LanguageId = languageId;
            Tags = tags;
            UsersWithVisibility = usersWithVisibility;
            VersionDescription = versionDescription.Trim();
        }
        public Guid CardId { get; }
        public Guid VersionCreatorId { get; set; }
        public string FrontSide { get; }
        public string BackSide { get; init; }
        public string AdditionalInfo { get; init; }
        public string References { get; init; }
        public Guid LanguageId { get; }
        public IEnumerable<Guid> Tags { get; init; }
        public IEnumerable<Guid> UsersWithVisibility { get; }
        public string VersionDescription { get; }
        public async Task CheckValidityAsync(CallContext callContext)
        {
            await CardInputValidator.RunAsync(this, callContext);

            await QueryValidationHelper.CheckUserExistsAsync(callContext.DbContext, VersionCreatorId);

            var card = await callContext.DbContext.Cards
                .Include(card => card.VersionCreator)
                .Include(card => card.CardLanguage)
                .Include(card => card.TagsInCards)
                .ThenInclude(tag => tag.Tag)
                .Include(card => card.UsersWithView)
                .AsSingleQuery()
                .SingleOrDefaultAsync(card => card.Id == CardId);

            if (card == null)
                throw new ArgumentException("Unknown card id");

            CardVisibilityHelper.CheckUserIsAllowedToViewCard(callContext.DbContext, VersionCreatorId, CardId);

            CheckAtLeastOneFieldDifferent(card, callContext.Localized);

            await CheckNewVisibilityAsync(card, callContext.Localized, callContext.DbContext);
        }
    }
    public sealed record Result;
    #endregion
}
