using MemCheck.Application.Cards;
using System;
using System.Linq;

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
        private const int maxImageCountPerSide = 10;
        #endregion
        public const int MinVersionDescriptionLength = 3;
        public const int MaxVersionDescriptionLength = 1000;
        public const int MinReferencesLength = 0;
        public const int MaxReferencesLength = 4000;
        public static void Run(ICardInput input, ILocalized localizer)
        {
            if (QueryValidationHelper.IsReservedGuid(input.VersionCreatorId))
                throw new InvalidOperationException(localizer.GetLocalized("InvalidOwner"));

            if (input.FrontSide != input.FrontSide.Trim())
                throw new InvalidOperationException("Invalid front side: not trimmed");
            if (input.FrontSide.Length < minFrontSideLength || input.FrontSide.Length > maxFrontSideLength)
                throw new RequestInputException(localizer.GetLocalized("InvalidFrontSideLength") + $" {input.FrontSide.Length}" + localizer.GetLocalized("MustBeBetween") + $" {minFrontSideLength} " + localizer.GetLocalized("And") + $" {maxFrontSideLength}");
            if (input.FrontSideImageList.Count() > maxImageCountPerSide)
                throw new RequestInputException(localizer.GetLocalized("InvalidFrontSideImageCount") + $" {input.FrontSideImageList.Count()}" + localizer.GetLocalized("MustBeNotBeAvove") + $" {maxImageCountPerSide}");

            if (input.BackSide != input.BackSide.Trim())
                throw new InvalidOperationException("Invalid back side: not trimmed");
            if ((input.BackSide.Length < minBackSideLength || input.BackSide.Length > maxBackSideLength) && !(input.BackSide.Length == 0 && input.BackSideImageList.Any()))
                throw new RequestInputException(localizer.GetLocalized("InvalidBackSideLength") + $" {input.BackSide.Length}" + localizer.GetLocalized("MustBeBetween") + $" {minBackSideLength} " + localizer.GetLocalized("And") + $" {maxBackSideLength}");
            if (input.BackSideImageList.Count() > maxImageCountPerSide)
                throw new RequestInputException(localizer.GetLocalized("InvalidBackSideImageCount") + $" {input.BackSideImageList.Count()}" + localizer.GetLocalized("MustBeNotBeAvove") + $" {maxImageCountPerSide}");

            if (input.AdditionalInfo != input.AdditionalInfo.Trim())
                throw new InvalidOperationException("Invalid additional info: not trimmed");
            if (input.AdditionalInfo.Length < minAdditionalInfoLength || input.AdditionalInfo.Length > maxAdditionalInfoLength)
                throw new RequestInputException(localizer.GetLocalized("InvalidAdditionalInfoLength") + $" {input.AdditionalInfo.Length}" + localizer.GetLocalized("MustBeBetween") + $" {minAdditionalInfoLength} " + localizer.GetLocalized("And") + $" {maxAdditionalInfoLength}");
            if (input.AdditionalInfoImageList.Count() > maxImageCountPerSide)
                throw new RequestInputException(localizer.GetLocalized("InvalidAdditionalInfoImageCount") + $" {input.AdditionalInfoImageList.Count()}" + localizer.GetLocalized("MustBeNotBeAvove") + $" {maxImageCountPerSide}");

            if (input.References != input.References.Trim())
                throw new InvalidOperationException("Invalid References: not trimmed");
            if (input.References.Length < MinReferencesLength || input.References.Length > MaxReferencesLength)
                throw new RequestInputException(localizer.GetLocalized("InvalidReferencesLength") + $" {input.References.Length}" + localizer.GetLocalized("MustBeBetween") + $" {MinReferencesLength} " + localizer.GetLocalized("And") + $" {MaxReferencesLength}");

            if (input.VersionDescription != input.VersionDescription.Trim())
                throw new InvalidOperationException("Invalid VersionDescription: not trimmed");
            if (input.VersionDescription.Length < MinVersionDescriptionLength || input.VersionDescription.Length > MaxVersionDescriptionLength)
                throw new RequestInputException(localizer.GetLocalized("InvalidVersionDescriptionLength") + $" {input.VersionDescription.Length}" + localizer.GetLocalized("MustBeBetween") + $" {MinVersionDescriptionLength} " + localizer.GetLocalized("And") + $" {MaxVersionDescriptionLength}");


            var unionedImageLists = input.FrontSideImageList.Concat(input.BackSideImageList).Concat(input.AdditionalInfoImageList);
            if (unionedImageLists.GroupBy(guid => guid).Where(guid => guid.Count() > 1).Any())
                throw new RequestInputException(localizer.GetLocalized("ImageDuplicated"));

            if (QueryValidationHelper.IsReservedGuid(input.LanguageId))
                throw new RequestInputException(localizer.GetLocalized("InvalidInputLanguage"));

            if (input.Tags.Where(tag => QueryValidationHelper.IsReservedGuid(tag)).Any())
                throw new RequestInputException(localizer.GetLocalized("InvalidTag"));

            if (input.UsersWithVisibility.Where(userWithVisibility => QueryValidationHelper.IsReservedGuid(userWithVisibility)).Any())
                throw new RequestInputException(localizer.GetLocalized("InvalidUserWithVisibility"));

            if (!CardVisibilityHelper.CardIsVisibleToUser(input.VersionCreatorId, input.UsersWithVisibility))
                throw new InvalidOperationException(localizer.GetLocalized("OwnerMustHaveVisibility"));
        }
    }
}
