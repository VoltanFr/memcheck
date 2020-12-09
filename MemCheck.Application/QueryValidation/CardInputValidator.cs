using System;
using System.Collections.Generic;
using System.Linq;
using MemCheck.Application.CardChanging;
using MemCheck.Domain;
using Microsoft.Extensions.Localization;

namespace MemCheck.Application.QueryValidation
{
    internal static class CardInputValidator
    {
        #region Fields
        private const int minFrontSideLength = 3;
        private const int maxFrontSideLength = 5000;
        private const int minBackSideLength = 1;    //A digit may be ok
        private const int maxBackSideLength = 5000;
        private const int minAdditionalInfoLength = 0;
        private const int maxAdditionalInfoLength = 20000;
        private const int minVersionDescriptionLength = 3;
        private const int maxVersionDescriptionLength = 1000;
        private const int maxImageCountPerSide = 10;
        #endregion
        public static void Run(ICardInput input, IStringLocalizer localizer)
        {
            if (QueryValidationHelper.IsReservedGuid(input.VersionCreatorId))
                throw new RequestInputException(localizer["InvalidOwner"].Value);

            if (input.FrontSide != input.FrontSide.Trim())
                throw new InvalidOperationException("Invalid front side: not trimmed");
            if (input.FrontSide.Length < minFrontSideLength || input.FrontSide.Length > maxFrontSideLength)
                throw new RequestInputException(localizer["InvalidFrontSideLength"] + $" {input.FrontSide.Length}" + localizer["MustBeBetween"] + $" {minFrontSideLength} " + localizer["And"] + $" {maxFrontSideLength}");
            if (input.FrontSideImageList.Count() > maxImageCountPerSide)
                throw new RequestInputException(localizer["InvalidFrontSideImageCount"] + $" {input.FrontSideImageList.Count()}" + localizer["MustBeNotBeAvove"] + $" {maxImageCountPerSide}");

            if (input.BackSide != input.BackSide.Trim())
                throw new InvalidOperationException("Invalid back side: not trimmed");
            if ((input.BackSide.Length < minBackSideLength || input.BackSide.Length > maxBackSideLength) && !(input.BackSide.Length == 0 && input.BackSideImageList.Any()))
                throw new RequestInputException(localizer["InvalidBackSideLength"] + $" {input.BackSide.Length}" + localizer["MustBeBetween"] + $" {minBackSideLength} " + localizer["And"] + $" {maxBackSideLength}");
            if (input.BackSideImageList.Count() > maxImageCountPerSide)
                throw new RequestInputException(localizer["InvalidBackSideImageCount"] + $" {input.BackSideImageList.Count()}" + localizer["MustBeNotBeAvove"] + $" {maxImageCountPerSide}");

            if (input.AdditionalInfo != input.AdditionalInfo.Trim())
                throw new InvalidOperationException("Invalid additional info: not trimmed");
            if (input.AdditionalInfo.Length < minAdditionalInfoLength || input.AdditionalInfo.Length > maxAdditionalInfoLength)
                throw new RequestInputException(localizer["InvalidAdditionalInfoLength"] + $" {input.AdditionalInfo.Length}" + localizer["MustBeBetween"] + $" {minAdditionalInfoLength} " + localizer["And"] + $" {maxAdditionalInfoLength}");
            if (input.AdditionalInfoImageList.Count() > maxImageCountPerSide)
                throw new RequestInputException(localizer["InvalidAdditionalInfoImageCount"] + $" {input.AdditionalInfoImageList.Count()}" + localizer["MustBeNotBeAvove"] + $" {maxImageCountPerSide}");

            if (input.VersionDescription != input.VersionDescription.Trim())
                throw new InvalidOperationException("Invalid VersionDescription: not trimmed");
            if (input.VersionDescription.Length < minVersionDescriptionLength || input.VersionDescription.Length > maxVersionDescriptionLength)
                throw new RequestInputException(localizer["InvalidVersionDescriptionLength"] + $" {input.VersionDescription.Length}" + localizer["MustBeBetween"] + $" {minVersionDescriptionLength} " + localizer["And"] + $" {maxVersionDescriptionLength}");


            var unionedImageLists = input.FrontSideImageList.Concat(input.BackSideImageList).Concat(input.AdditionalInfoImageList);
            if (unionedImageLists.GroupBy(guid => guid).Where(guid => guid.Count() > 1).Any())
                throw new RequestInputException(localizer["ImageDuplicated"].Value);

            if (QueryValidationHelper.IsReservedGuid(input.LanguageId))
                throw new RequestInputException(localizer["InvalidInputLanguage"].Value);

            if (input.Tags.Where(tag => QueryValidationHelper.IsReservedGuid(tag)).Any())
                throw new RequestInputException(localizer["InvalidTag"].Value);

            if (input.UsersWithVisibility.Where(userWithVisibility => QueryValidationHelper.IsReservedGuid(userWithVisibility)).Any())
                throw new RequestInputException(localizer["InvalidUserWithVisibility"].Value);

            if (!CardVisibilityHelper.CardIsVisibleToUser(input.VersionCreatorId, input.UsersWithVisibility))
                //To be reviewed when we support card versions: I suspect we want visibility for all past owners
                throw new RequestInputException(localizer["OwnerMustHaveVisibility"].Value);
        }
    }
}
