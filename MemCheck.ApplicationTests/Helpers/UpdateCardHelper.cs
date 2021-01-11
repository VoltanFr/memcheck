using System;
using MemCheck.Domain;
using System.Collections.Generic;
using MemCheck.Application.CardChanging;
using System.Linq;

namespace MemCheck.Application.Tests.Helpers
{
    public static class UpdateCardHelper
    {
        public static UpdateCard.Request RequestForTagChanges(Card card, IEnumerable<Guid> tagIds)
        {
            return new UpdateCard.Request(
                card.Id,
                card.VersionCreator.Id,
                card.FrontSide,
                card.Images.Where(i => i.CardSide == ImageInCard.FrontSide).Select(i => i.ImageId),
                card.BackSide,
                card.Images.Where(i => i.CardSide == ImageInCard.BackSide).Select(i => i.ImageId),
                card.AdditionalInfo,
                card.Images.Where(i => i.CardSide == ImageInCard.AdditionalInfo).Select(i => i.ImageId),
                card.CardLanguage.Id,
                tagIds,
                card.UsersWithView.Select(uwv => uwv.UserId),
                StringHelper.RandomString()
                );
        }
        public static UpdateCard.Request RequestForVisibilityChanges(Card card, IEnumerable<Guid> userWithViewIds, Guid? versionCreator = null)
        {
            return new UpdateCard.Request(
                card.Id,
                 versionCreator ?? card.VersionCreator.Id,
                card.FrontSide,
                card.Images.Where(i => i.CardSide == ImageInCard.FrontSide).Select(i => i.ImageId),
                card.BackSide,
                card.Images.Where(i => i.CardSide == ImageInCard.BackSide).Select(i => i.ImageId),
                card.AdditionalInfo,
                card.Images.Where(i => i.CardSide == ImageInCard.AdditionalInfo).Select(i => i.ImageId),
                card.CardLanguage.Id,
                card.TagsInCards.Select(t => t.TagId),
                userWithViewIds,
                StringHelper.RandomString()
                );
        }
        public static UpdateCard.Request RequestForFrontSideChanges(Card card, string frontSide, Guid? versionCreator = null, string? versionDescription = null)
        {
            return new UpdateCard.Request(
                card.Id,
                versionCreator ?? card.VersionCreator.Id,
                frontSide,
                card.Images.Where(i => i.CardSide == ImageInCard.FrontSide).Select(i => i.ImageId),
                card.BackSide,
                card.Images.Where(i => i.CardSide == ImageInCard.BackSide).Select(i => i.ImageId),
                card.AdditionalInfo,
                card.Images.Where(i => i.CardSide == ImageInCard.AdditionalInfo).Select(i => i.ImageId),
                card.CardLanguage.Id,
                card.TagsInCards.Select(t => t.TagId),
                card.UsersWithView.Select(uwv => uwv.UserId),
                versionDescription ?? StringHelper.RandomString()
                );
        }
        public static UpdateCard.Request RequestForBackSideChanges(Card card, string backSide, Guid? versionCreator = null, string? versionDescription = null)
        {
            return new UpdateCard.Request(
                card.Id,
                versionCreator ?? card.VersionCreator.Id,
                card.FrontSide,
                card.Images.Where(i => i.CardSide == ImageInCard.FrontSide).Select(i => i.ImageId),
                backSide,
                card.Images.Where(i => i.CardSide == ImageInCard.BackSide).Select(i => i.ImageId),
                card.AdditionalInfo,
                card.Images.Where(i => i.CardSide == ImageInCard.AdditionalInfo).Select(i => i.ImageId),
                card.CardLanguage.Id,
                card.TagsInCards.Select(t => t.TagId),
                card.UsersWithView.Select(uwv => uwv.UserId),
                versionDescription ?? StringHelper.RandomString()
                );
        }
    }
}
