using MemCheck.Application.Notifiying;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards;

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
    private static CardPreviousVersionType CardPreviousVersionTypeFromCard(Card c)
    {
        return c.VersionType switch
        {
            CardVersionType.Creation => CardPreviousVersionType.Creation,
            CardVersionType.Changes => CardPreviousVersionType.Changes,
            _ => throw new NotImplementedException(),
        };
    }
    private async Task<CardPreviousVersion> CreatePreviousVersionAsync(Card card, DateTime? versionUtcDate = null)
    {
        var previousVersion = new CardPreviousVersion()
        {
            Card = card.Id,
            VersionCreator = card.VersionCreator,
            CardLanguage = card.CardLanguage,
            FrontSide = card.FrontSide,
            BackSide = card.BackSide,
            AdditionalInfo = card.AdditionalInfo,
            References = card.References,
            VersionUtcDate = versionUtcDate ?? card.VersionUtcDate,
            VersionType = CardPreviousVersionTypeFromCard(card),
            VersionDescription = card.VersionDescription,
            PreviousVersion = card.PreviousVersion,
        };
        await dbContext.CardPreviousVersions.AddAsync(previousVersion);
        await CreatePreviousVersionTagsAsync(previousVersion, card.TagsInCards);
        await CreatePreviousVersionUsersWithViewAsync(previousVersion, card.UsersWithView);

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
            .Include(card => card.UsersWithView)
            .ThenInclude(userWithView => userWithView.User)
            .Include(card => card.PreviousVersion)
            .Include(card => card.CardLanguage)
            .Include(card => card.TagsInCards)
            .Include(card => card.VersionCreator)
            //.AsSingleQuery()
            .SingleAsync(card => card.Id == cardId);

        var previousVersion = await CreatePreviousVersionAsync(card);
        card.PreviousVersion = previousVersion;

        var newVersionCreator = dbContext.Users.Single(u => u.Id == newVersionCreatorId);
        card.VersionCreator = newVersionCreator;

        card.VersionUtcDate = cardNewVersionUtcDate ?? DateTime.UtcNow;
        card.VersionDescription = newVersionDescription;
        card.VersionType = CardVersionType.Changes;

        if (newVersionCreator.SubscribeToCardOnEdit)
            AddCardSubscriptions.CreateSubscription(dbContext, newVersionCreator.Id, cardId, card.VersionUtcDate, CardNotificationSubscription.CardNotificationRegistrationMethodVersionCreation);

        return card;
    }
    public async Task RunForDeletionAsync(Card card, DateTime? versionUtcDate = null)
    {
        var previousVersion = await CreatePreviousVersionAsync(card, versionUtcDate);
        previousVersion.VersionType = CardPreviousVersionType.Deletion;
    }
}
