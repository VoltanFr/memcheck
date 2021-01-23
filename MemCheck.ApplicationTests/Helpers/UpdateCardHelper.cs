using MemCheck.Application.CardChanging;
using MemCheck.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MemCheck.Application.Tests.Helpers
{
    public static class UpdateCardHelper
    {
        public static UpdateCard.Request RequestForTagChange(Card card, IEnumerable<Guid> tagIds, string? versionDescription = null)
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
                versionDescription ?? RandomHelper.String()
                );
        }
        public static UpdateCard.Request RequestForVisibilityChange(Card card, IEnumerable<Guid> userWithViewIds, Guid? versionCreator = null, string? versionDescription = null)
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
                versionDescription ?? RandomHelper.String()
                );
        }
        public static UpdateCard.Request RequestForFrontSideChange(Card card, string frontSide, Guid? versionCreator = null, string? versionDescription = null)
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
                versionDescription ?? RandomHelper.String()
                );
        }
        public static UpdateCard.Request RequestForBackSideChange(Card card, string backSide, Guid? versionCreator = null, string? versionDescription = null)
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
                versionDescription ?? RandomHelper.String()
                );
        }
        public static UpdateCard.Request RequestForAdditionalInfoChange(Card card, string additionalInfo, Guid? versionCreator = null, string? versionDescription = null)
        {
            return new UpdateCard.Request(
                card.Id,
                versionCreator ?? card.VersionCreator.Id,
                card.FrontSide,
                card.Images.Where(i => i.CardSide == ImageInCard.FrontSide).Select(i => i.ImageId),
                card.BackSide,
                card.Images.Where(i => i.CardSide == ImageInCard.BackSide).Select(i => i.ImageId),
                additionalInfo,
                card.Images.Where(i => i.CardSide == ImageInCard.AdditionalInfo).Select(i => i.ImageId),
                card.CardLanguage.Id,
                card.TagsInCards.Select(t => t.TagId),
                card.UsersWithView.Select(uwv => uwv.UserId),
                versionDescription ?? RandomHelper.String()
                );
        }
        public static UpdateCard.Request RequestForLanguageChange(Card card, Guid newLanguageId, Guid? versionCreator = null, string? versionDescription = null)
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
                newLanguageId,
                card.TagsInCards.Select(t => t.TagId),
                card.UsersWithView.Select(uwv => uwv.UserId),
                versionDescription ?? RandomHelper.String()
                );
        }
        public static UpdateCard.Request RequestForFrontSideImageChange(Card card, Guid[] images, Guid? versionCreator = null, string? versionDescription = null)
        {
            return new UpdateCard.Request(
                card.Id,
                versionCreator ?? card.VersionCreator.Id,
                card.FrontSide,
                images,
                card.BackSide,
                card.Images.Where(i => i.CardSide == ImageInCard.BackSide).Select(i => i.ImageId),
                card.AdditionalInfo,
                card.Images.Where(i => i.CardSide == ImageInCard.AdditionalInfo).Select(i => i.ImageId),
                card.CardLanguage.Id,
                card.TagsInCards.Select(t => t.TagId),
                card.UsersWithView.Select(uwv => uwv.UserId),
                versionDescription ?? RandomHelper.String()
                );
        }
        public static UpdateCard.Request RequestForBackSideImageChange(Card card, IEnumerable<Guid> images, Guid? versionCreator = null, string? versionDescription = null)
        {
            return new UpdateCard.Request(
                card.Id,
                versionCreator ?? card.VersionCreator.Id,
                card.FrontSide,
                card.Images.Where(i => i.CardSide == ImageInCard.FrontSide).Select(i => i.ImageId),
                card.BackSide,
                images,
                card.AdditionalInfo,
                card.Images.Where(i => i.CardSide == ImageInCard.AdditionalInfo).Select(i => i.ImageId),
                card.CardLanguage.Id,
                card.TagsInCards.Select(t => t.TagId),
                card.UsersWithView.Select(uwv => uwv.UserId),
                versionDescription ?? RandomHelper.String()
                );
        }
        public static UpdateCard.Request RequestForAdditionalSideImageChange(Card card, IEnumerable<Guid> images, Guid? versionCreator = null, string? versionDescription = null)
        {
            return new UpdateCard.Request(
                card.Id,
                versionCreator ?? card.VersionCreator.Id,
                card.FrontSide,
                card.Images.Where(i => i.CardSide == ImageInCard.FrontSide).Select(i => i.ImageId),
                card.BackSide,
                card.Images.Where(i => i.CardSide == ImageInCard.BackSide).Select(i => i.ImageId),
                card.AdditionalInfo,
                images,
                card.CardLanguage.Id,
                card.TagsInCards.Select(t => t.TagId),
                card.UsersWithView.Select(uwv => uwv.UserId),
                versionDescription ?? RandomHelper.String()
                );
        }
    }
}
