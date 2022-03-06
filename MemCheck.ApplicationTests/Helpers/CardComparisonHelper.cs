using MemCheck.Application.QueryValidation;
using MemCheck.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MemCheck.Application.Tests.Helpers
{
    public static class CardComparisonHelper
    {
        #region private record ImageWithSide
        private record ImageWithSide(Guid ImageId, int CardSide);
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
        public static void AssertSameTagSet(IEnumerable<TagInCard> cardTags, IEnumerable<TagInPreviousCardVersion> cardPreviousVersionTags)
        {
            var cardTagSet = cardTags.Select(t => t.TagId).ToHashSet();
            var cardPreviousVersionTagSet = cardPreviousVersionTags.Select(t => t.TagId).ToHashSet();
            Assert.IsTrue(cardTagSet.SetEquals(cardPreviousVersionTagSet));
        }
        public static void AssertSameUserWithViewSet(IEnumerable<UserWithViewOnCard> cardAllowedUsers, IEnumerable<UserWithViewOnCardPreviousVersion> cardPreviousVersionAllowedUsers)
        {
            Assert.IsTrue(CardVisibilityHelper.CardsHaveSameUsersWithView(cardAllowedUsers, cardPreviousVersionAllowedUsers));
        }
        public static void AssertSameImageSet(IEnumerable<ImageInCard> cardImages, IEnumerable<ImageInCardPreviousVersion> cardPreviousVersionImages)
        {
            var cardImageSet = cardImages.Select(i => new ImageWithSide(i.ImageId, i.CardSide)).ToHashSet();
            var cardPreviousVersionImageSet = cardPreviousVersionImages.Select(i => new ImageWithSide(i.ImageId, i.CardSide)).ToHashSet();
            Assert.IsTrue(cardImageSet.SetEquals(cardPreviousVersionImageSet));
        }
    }
}
