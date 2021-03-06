﻿using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards
{
    public sealed class UpdateCard
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        #region Private methods
        private async Task UpdateUsersWithViewAsync(Request request, Card card)
        {
            //We could do better than the current implem: just add the needed users and remove the unwanted

            var usersWithVisibilityBeforeUpdate = dbContext.UsersWithViewOnCards.Where(userWithView => userWithView.CardId == request.CardId);
            dbContext.UsersWithViewOnCards.RemoveRange(usersWithVisibilityBeforeUpdate);

            var usersWithView = new List<UserWithViewOnCard>();
            foreach (var userFromRequestId in request.UsersWithVisibility)
            {
                var userFromDb = dbContext.Users.Where(u => u.Id == userFromRequestId).Single();
                var userWithView = new UserWithViewOnCard() { UserId = userFromDb.Id, User = userFromDb, CardId = card.Id, Card = card };
                await dbContext.UsersWithViewOnCards.AddAsync(userWithView);
                usersWithView.Add(userWithView);
            }

            card.UsersWithView = usersWithView;
        }
        private async Task UpdateTagsAsync(Request request, Card card)
        {
            var tagsBeforeUpdate = dbContext.TagsInCards.Where(tag => tag.CardId == request.CardId);
            dbContext.TagsInCards.RemoveRange(tagsBeforeUpdate);

            var tagsInCards = new List<TagInCard>();
            foreach (var tagToAdd in request.Tags)
            {
                var tagFromDb = dbContext.Tags.Where(t => t.Id == tagToAdd).Single();
                var tagInCard = new TagInCard() { TagId = tagFromDb.Id, Tag = tagFromDb, CardId = card.Id };
                await dbContext.TagsInCards.AddAsync(tagInCard);
                tagsInCards.Add(tagInCard);
            }
            card.TagsInCards = tagsInCards;
        }
        private async Task UpdateImagesAsync(Request request, Card card)
        {
            var imagesBeforeUpdate = dbContext.ImagesInCards.Where(image => image.CardId == request.CardId);
            dbContext.ImagesInCards.RemoveRange(imagesBeforeUpdate);

            var cardImageList = new List<ImageInCard>();
            foreach (var image in request.FrontSideImageList)
                await AddImageAsync(card.Id, image, 1, cardImageList);
            foreach (var image in request.BackSideImageList)
                await AddImageAsync(card.Id, image, 2, cardImageList);
            foreach (var image in request.AdditionalInfoImageList)
                await AddImageAsync(card.Id, image, 3, cardImageList);

            card.Images = cardImageList;
        }
        private async Task AddImageAsync(Guid cardId, Guid imageId, int cardSide, List<ImageInCard> cardImageList)
        {
            var imageFromDb = dbContext.Images.Where(img => img.Id == imageId).Single();    //To be reviewed: it sounds stupid that we have to load the whole image info, with the blob, while we only need an id???
            var img = new ImageInCard() { ImageId = imageId, Image = imageFromDb, CardId = cardId, CardSide = cardSide };
            await dbContext.ImagesInCards.AddAsync(img);
            cardImageList.Add(img);
        }
        #endregion
        public UpdateCard(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task RunAsync(Request request, ILocalized localizer, DateTime? cardNewVersionUtcDate = null)
        {
            await request.CheckValidityAsync(localizer, dbContext);

            var previousVersionCreator = new PreviousVersionCreator(dbContext);
            var card = await previousVersionCreator.RunAsync(request.CardId, request.VersionCreatorId, request.VersionDescription, cardNewVersionUtcDate);

            card.CardLanguage = dbContext.CardLanguages.Where(language => language.Id == request.LanguageId).Single();
            card.FrontSide = request.FrontSide;
            card.BackSide = request.BackSide;
            card.AdditionalInfo = request.AdditionalInfo;
            await UpdateTagsAsync(request, card);
            await UpdateUsersWithViewAsync(request, card);
            await UpdateImagesAsync(request, card);

            await dbContext.SaveChangesAsync();
        }
        #region Request class
        public sealed record Request : ICardInput
        {
            #region Private methods
            private bool SameImageLists(IEnumerable<ImageInCard> originalImages)
            {
                return FrontSideImageList.SequenceEqual(originalImages.Where(img => img.CardSide == ImageInCard.FrontSide).Select(img => img.ImageId))
                    && BackSideImageList.SequenceEqual(originalImages.Where(img => img.CardSide == ImageInCard.BackSide).Select(img => img.ImageId))
                    && AdditionalInfoImageList.SequenceEqual(originalImages.Where(img => img.CardSide == ImageInCard.AdditionalInfo).Select(img => img.ImageId));
            }
            private void CheckAtLeastOneFieldDifferent(Card card, ILocalized localizer)
            {
                var dataBeforeUpdate = new
                {
                    CardLanguageId = card.CardLanguage.Id,
                    card.FrontSide,
                    card.BackSide,
                    card.AdditionalInfo,
                    TagIds = card.TagsInCards.Select(tag => tag.TagId),
                    UserWithVisibilityIds = card.UsersWithView.Select(u => u.UserId),
                    card.Images
                };

                if ((dataBeforeUpdate.CardLanguageId == LanguageId)
                    && (dataBeforeUpdate.FrontSide == FrontSide)
                    && (dataBeforeUpdate.BackSide == BackSide)
                    && (dataBeforeUpdate.AdditionalInfo == AdditionalInfo)
                    && Enumerable.SequenceEqual(dataBeforeUpdate.TagIds.OrderBy(tagId => tagId), Tags.OrderBy(tagId => tagId))
                    && Enumerable.SequenceEqual(dataBeforeUpdate.UserWithVisibilityIds.OrderBy(userId => userId), UsersWithVisibility.OrderBy(userId => userId))
                    && SameImageLists(dataBeforeUpdate.Images))
                    throw new RequestInputException(localizer.Get("CanNotUpdateBecauseNoDifference"));
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
                        throw new RequestInputException($"{localizer.Get("User")} {user.UserName} {localizer.Get("MustHaveVisibilityBecause")} {localizer.Get(messageSuffixId)}");
                    }
            }
            private async Task CheckNewVisibilityAsync(Card card, ILocalized localizer, MemCheckDbContext dbContext)
            {
                if (!UsersWithVisibility.Any())
                    return;
                await CheckVisibilityForUsersAsync(await GetAllAuthorsAsync(card, dbContext), localizer, "HeCreatedAVersionOfThisCard", dbContext);
                IEnumerable<Guid> userIds = await GetAllUsersWithCardInADeckAsync(dbContext);
                await CheckVisibilityForUsersAsync(userIds, localizer, "HeHasThisCardInADeck", dbContext);
            }
            #endregion
            public Request(Guid cardId, Guid versionCreatorId, string frontSide, IEnumerable<Guid> frontSideImageList, string backSide, IEnumerable<Guid> backSideImageList, string additionalInfo, IEnumerable<Guid> additionalInfoImageList, Guid languageId, IEnumerable<Guid> tags, IEnumerable<Guid> usersWithVisibility, string versionDescription)
            {
                CardId = cardId;
                VersionCreatorId = versionCreatorId;
                FrontSide = frontSide.Trim();
                BackSide = backSide.Trim();
                AdditionalInfo = additionalInfo.Trim();
                FrontSideImageList = frontSideImageList;
                BackSideImageList = backSideImageList;
                AdditionalInfoImageList = additionalInfoImageList;
                LanguageId = languageId;
                Tags = tags;
                UsersWithVisibility = usersWithVisibility;
                VersionDescription = versionDescription.Trim();
            }
            public Guid CardId { get; }
            public Guid VersionCreatorId { get; set; }
            public string FrontSide { get; }
            public IEnumerable<Guid> FrontSideImageList { get; }
            public string BackSide { get; init; }
            public IEnumerable<Guid> BackSideImageList { get; }
            public string AdditionalInfo { get; init; }
            public IEnumerable<Guid> AdditionalInfoImageList { get; init; }
            public Guid LanguageId { get; }
            public IEnumerable<Guid> Tags { get; init; }
            public IEnumerable<Guid> UsersWithVisibility { get; }
            public string VersionDescription { get; }
            public async Task CheckValidityAsync(ILocalized localizer, MemCheckDbContext dbContext)
            {
                CardInputValidator.Run(this, localizer);

                var cards = dbContext.Cards
                    .Include(card => card.VersionCreator)
                    .Include(card => card.CardLanguage)
                    .Include(card => card.Images)
                    .Include(card => card.TagsInCards)
                    .ThenInclude(tag => tag.Tag)
                    .Include(card => card.UsersWithView)
                    .AsSingleQuery()
                    .Where(card => card.Id == CardId);

                if (!await cards.AnyAsync())
                    throw new ApplicationException("Unknown card id");

                CardVisibilityHelper.CheckUserIsAllowedToViewCards(dbContext, VersionCreatorId, CardId);

                Card card = cards.Single();

                CheckAtLeastOneFieldDifferent(card, localizer);

                await CheckNewVisibilityAsync(card, localizer, dbContext);
            }
        }
        #endregion
    }
}
