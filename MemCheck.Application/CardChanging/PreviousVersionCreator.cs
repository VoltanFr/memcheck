using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MemCheck.Application.Notifying;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;

namespace MemCheck.Application.CardChanging
{
    internal sealed class PreviousVersionCreator
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        #region Private methods
        private async Task CreatePreviousVersionTagsAsync(CardPreviousVersion cardPreviousVersion, IEnumerable<TagInCard> originalCardTags)
        {
            var tagsInCards = new List<TagInPreviousCardVersion>();
            foreach (var tagToAdd in originalCardTags)
            {
                var tagInCard = new TagInPreviousCardVersion() { TagId = tagToAdd.TagId, CardPreviousVersionId = cardPreviousVersion.Id };
                await dbContext.TagInPreviousCardVersions.AddAsync(tagInCard);
                tagsInCards.Add(tagInCard);
            }
            cardPreviousVersion.Tags = tagsInCards;
        }
        private async Task CreatePreviousVersionUsersWithViewAsync(CardPreviousVersion cardPreviousVersion, IEnumerable<UserWithViewOnCard> originalCardUsers)
        {
            var usersInCards = new List<UserWithViewOnCardPreviousVersion>();
            foreach (var userToAdd in originalCardUsers)
            {
                var userInCard = new UserWithViewOnCardPreviousVersion() { AllowedUserId = userToAdd.UserId, CardPreviousVersionId = cardPreviousVersion.Id };
                await dbContext.UsersWithViewOnCardPreviousVersions.AddAsync(userInCard);
                usersInCards.Add(userInCard);
            }
            cardPreviousVersion.UsersWithView = usersInCards;
        }
        private async Task CreatePreviousVersionImagesWithViewAsync(CardPreviousVersion cardPreviousVersion, IEnumerable<ImageInCard> originalCardImages)
        {
            var imagesInCards = new List<ImageInCardPreviousVersion>();
            foreach (var imageToAdd in originalCardImages)
            {
                var imageInCard = new ImageInCardPreviousVersion() { ImageId = imageToAdd.ImageId, CardPreviousVersionId = cardPreviousVersion.Id, CardSide = imageToAdd.CardSide };
                await dbContext.ImagesInCardPreviousVersions.AddAsync(imageInCard);
                imagesInCards.Add(imageInCard);
            }
            cardPreviousVersion.Images = imagesInCards;
        }
        private CardPreviousVersionType CardPreviousVersionTypeFromCard(Card c)
        {
            switch (c.VersionType)
            {
                case CardVersionType.Creation:
                    return CardPreviousVersionType.Creation;
                case CardVersionType.Changes:
                    return CardPreviousVersionType.Changes;
                default:
                    throw new NotImplementedException();
            }
        }
        private async Task<CardPreviousVersion> CreatePreviousVersionAsync(Card card)
        {
            var previousVersion = new CardPreviousVersion()
            {
                Card = card.Id,
                VersionCreator = card.VersionCreator,
                CardLanguage = card.CardLanguage,
                FrontSide = card.FrontSide,
                BackSide = card.BackSide,
                AdditionalInfo = card.AdditionalInfo,
                VersionUtcDate = card.VersionUtcDate,
                VersionType = CardPreviousVersionTypeFromCard(card),
                VersionDescription = card.VersionDescription,
                PreviousVersion = card.PreviousVersion,
            };
            await dbContext.CardPreviousVersions.AddAsync(previousVersion);
            await CreatePreviousVersionTagsAsync(previousVersion, card.TagsInCards);
            await CreatePreviousVersionUsersWithViewAsync(previousVersion, card.UsersWithView);
            await CreatePreviousVersionImagesWithViewAsync(previousVersion, card.Images);

            return previousVersion;
        }
        #endregion
        public PreviousVersionCreator(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task<Card> RunAsync(Guid cardId, Guid newVersionCreatorId, string newVersionDescription, DateTime? cardNewVersionUtcDate = null)
        {
            var card = await dbContext.Cards
                .Include(card => card.Images)
                .Include(card => card.UsersWithView)
                .ThenInclude(userWithView => userWithView.User)
                .Include(card => card.PreviousVersion)
                .Include(card => card.CardLanguage)
                .Include(card => card.TagsInCards)
                .Include(card => card.VersionCreator)
                //.AsSingleQuery()
                .SingleAsync(img => img.Id == cardId);

            var previousVersion = await CreatePreviousVersionAsync(card);

            var newVersionCreator = dbContext.Users.Single(u => u.Id == newVersionCreatorId);

            card.PreviousVersion = previousVersion;
            card.VersionCreator = newVersionCreator;
            card.VersionUtcDate = cardNewVersionUtcDate ?? DateTime.UtcNow;
            card.VersionDescription = newVersionDescription;
            card.VersionType = CardVersionType.Changes;

            if (newVersionCreator.SubscribeToCardOnEdit)
                AddCardSubscriptions.CreateSubscription(dbContext, newVersionCreator.Id, cardId, card.VersionUtcDate, CardNotificationSubscription.CardNotificationRegistrationMethod_VersionCreation);

            return card;
        }
        public async Task RunForDeletionAsync(Card card, DateTime? versionUtcDate = null)
        {
            var previousVersion = await CreatePreviousVersionAsync(card);
            previousVersion.VersionType = CardPreviousVersionType.Deletion;
        }
    }
}
