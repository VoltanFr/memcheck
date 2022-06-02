using MemCheck.Application.QueryValidation;
using MemCheck.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MemCheck.Application.Helpers;

public static class CardComparisonHelper
{
    #region private record ImageWithSide
    private record ImageWithSide(Guid ImageId, int CardSide);
    private record UserWithRating(Guid UserId, int Rating);
    #endregion
    public static void AssertSameContent(Card card, CardPreviousVersion firstVersion, bool includeCreator)
    {
        if (includeCreator)
            Assert.AreEqual(card.VersionCreator.Id, firstVersion.VersionCreator.Id);
        Assert.AreEqual(card.CardLanguage.Id, firstVersion.CardLanguage.Id);
        Assert.AreEqual(card.FrontSide, firstVersion.FrontSide);
        Assert.AreEqual(card.BackSide, firstVersion.BackSide);
        Assert.AreEqual(card.AdditionalInfo, firstVersion.AdditionalInfo);
        Assert.AreEqual(card.References, firstVersion.References);
        AssertSameTagSet(card.TagsInCards, firstVersion.Tags);
        AssertSameUserWithViewSet(card.UsersWithView, firstVersion.UsersWithView);
        AssertSameImageSet(card.Images, firstVersion.Images);
    }
    public static void AssertSameContent(Card expectedCard, Card actualCard, bool versionCreator = true, bool frontSide = true, bool backSide = true, bool additionalInfo = true, bool references = true, bool versionDate = true, bool versionDescription = true, bool versionType = true, bool previousVersion = true)
    {
        if (versionCreator)
            Assert.AreEqual(expectedCard.VersionCreator.Id, actualCard.VersionCreator.Id);
        Assert.AreEqual(expectedCard.CardLanguage.Id, actualCard.CardLanguage.Id);
        if (frontSide)
            Assert.AreEqual(expectedCard.FrontSide, actualCard.FrontSide);
        if (backSide)
            Assert.AreEqual(expectedCard.BackSide, actualCard.BackSide);
        if (additionalInfo)
            Assert.AreEqual(expectedCard.AdditionalInfo, actualCard.AdditionalInfo);
        if (references)
            Assert.AreEqual(expectedCard.References, actualCard.References);
        AssertSameDeckSet(expectedCard.CardInDecks, actualCard.CardInDecks);
        AssertSameTagSet(expectedCard.TagsInCards, actualCard.TagsInCards);
        Assert.AreEqual(expectedCard.InitialCreationUtcDate, actualCard.InitialCreationUtcDate);
        if (versionDate)
            Assert.AreEqual(expectedCard.VersionUtcDate, actualCard.VersionUtcDate);
        AssertSameUserWithViewSet(expectedCard.UsersWithView, actualCard.UsersWithView);
        AssertSameImageSet(expectedCard.Images, actualCard.Images);
        if (versionDescription)
            Assert.AreEqual(expectedCard.VersionDescription, actualCard.VersionDescription);
        if (versionType)
            Assert.AreEqual(expectedCard.VersionType, actualCard.VersionType);
        if (previousVersion)
        {
            if (expectedCard.PreviousVersion == null)
                Assert.IsNull(actualCard.PreviousVersion);
            else
                if (actualCard.PreviousVersion == null)
                Assert.IsNull(expectedCard.PreviousVersion);
            else
                Assert.AreEqual(expectedCard.PreviousVersion.Id, actualCard.PreviousVersion.Id);
        }
        Assert.AreEqual(expectedCard.RatingCount, actualCard.RatingCount);
        Assert.AreEqual(expectedCard.AverageRating, actualCard.AverageRating);
        AssertSameRatingSet(expectedCard.UserCardRating, actualCard.UserCardRating);
    }
    public static void AssertSameDeckSet(IEnumerable<CardInDeck> cardsInDecks1, IEnumerable<CardInDeck> cardsInDecks2)
    {
        var card1InDeckSet = cardsInDecks1.Select(cardInDecks => cardInDecks.DeckId).ToHashSet();
        var card2InDeckSet = cardsInDecks2.Select(cardInDecks => cardInDecks.DeckId).ToHashSet();
        Assert.IsTrue(card1InDeckSet.SetEquals(card2InDeckSet));
    }
    public static void AssertSameTagSet(IEnumerable<TagInCard> cardTags, IEnumerable<TagInPreviousCardVersion> cardPreviousVersionTags)
    {
        var cardTagSet = cardTags.Select(t => t.TagId).ToHashSet();
        var cardPreviousVersionTagSet = cardPreviousVersionTags.Select(t => t.TagId).ToHashSet();
        Assert.IsTrue(cardTagSet.SetEquals(cardPreviousVersionTagSet));
    }
    public static void AssertSameTagSet(IEnumerable<TagInCard> card1Tags, IEnumerable<TagInCard> card2Tags)
    {
        var card1TagSet = card1Tags.Select(t => t.TagId).ToHashSet();
        var card2TagSet = card2Tags.Select(t => t.TagId).ToHashSet();
        Assert.IsTrue(card1TagSet.SetEquals(card2TagSet));
    }
    public static void AssertSameUserWithViewSet(IEnumerable<UserWithViewOnCard> cardAllowedUsers, IEnumerable<UserWithViewOnCardPreviousVersion> cardPreviousVersionAllowedUsers)
    {
        Assert.IsTrue(CardVisibilityHelper.CardsHaveSameUsersWithView(cardAllowedUsers, cardPreviousVersionAllowedUsers));
    }
    public static void AssertSameUserWithViewSet(IEnumerable<UserWithViewOnCard> card1AllowedUsers, IEnumerable<UserWithViewOnCard> card2AllowedUsers)
    {
        Assert.IsTrue(ComparisonHelper.SameSetOfGuid(card1AllowedUsers.Select(u => u.UserId), card2AllowedUsers.Select(u => u.UserId)));
    }
    public static void AssertSameImageSet(IEnumerable<ImageInCard> cardImages, IEnumerable<ImageInCardPreviousVersion> cardPreviousVersionImages)
    {
        var cardImageSet = cardImages.Select(i => new ImageWithSide(i.ImageId, i.CardSide)).ToHashSet();
        var cardPreviousVersionImageSet = cardPreviousVersionImages.Select(i => new ImageWithSide(i.ImageId, i.CardSide)).ToHashSet();
        Assert.IsTrue(cardImageSet.SetEquals(cardPreviousVersionImageSet));
    }
    public static void AssertSameImageSet(IEnumerable<ImageInCard> card1Images, IEnumerable<ImageInCard> card2Images)
    {
        var card1ImageSet = card1Images.Select(i => new ImageWithSide(i.ImageId, i.CardSide)).ToHashSet();
        var card2ImageSet = card2Images.Select(i => new ImageWithSide(i.ImageId, i.CardSide)).ToHashSet();
        Assert.IsTrue(card1ImageSet.SetEquals(card2ImageSet));
    }
    public static void AssertSameRatingSet(IEnumerable<UserCardRating> card1Ratings, IEnumerable<UserCardRating> card2Ratings)
    {
        var card1RatingSet = card1Ratings.Select(i => new UserWithRating(i.UserId, i.Rating)).ToHashSet();
        var card2RatingSet = card2Ratings.Select(i => new UserWithRating(i.UserId, i.Rating)).ToHashSet();
        Assert.IsTrue(card1RatingSet.SetEquals(card2RatingSet));
    }
}
