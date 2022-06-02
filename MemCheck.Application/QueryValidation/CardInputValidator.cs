using MemCheck.Application.Cards;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.QueryValidation;

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
    public const string ExceptionMesg_VersionDescriptionNotTrimmed = "Invalid VersionDescription: not trimmed";
    public static async Task RunAsync(ICardInput input, CallContext callContext)
    {
        await QueryValidationHelper.CheckUserExistsAsync(callContext.DbContext, input.VersionCreatorId);
        await QueryValidationHelper.CheckTagsExistAsync(input.Tags, callContext.DbContext);
        await QueryValidationHelper.CheckUsersExistAsync(callContext.DbContext, input.UsersWithVisibility);

        if (input.FrontSide != input.FrontSide.Trim())
            throw new InvalidOperationException("Invalid front side: not trimmed");
        if (input.FrontSide.Length is < minFrontSideLength or > maxFrontSideLength)
            throw new RequestInputException(callContext.Localized.GetLocalized("InvalidFrontSideLength") + $" {input.FrontSide.Length}" + callContext.Localized.GetLocalized("MustBeBetween") + $" {minFrontSideLength} " + callContext.Localized.GetLocalized("And") + $" {maxFrontSideLength}");
        if (input.FrontSideImageList.Count() > maxImageCountPerSide)
            throw new RequestInputException(callContext.Localized.GetLocalized("InvalidFrontSideImageCount") + $" {input.FrontSideImageList.Count()}" + callContext.Localized.GetLocalized("MustBeNotBeAvove") + $" {maxImageCountPerSide}");

        if (input.BackSide != input.BackSide.Trim())
            throw new InvalidOperationException("Invalid back side: not trimmed");
        if ((input.BackSide.Length < minBackSideLength || input.BackSide.Length > maxBackSideLength) && !(input.BackSide.Length == 0 && input.BackSideImageList.Any()))
            throw new RequestInputException(callContext.Localized.GetLocalized("InvalidBackSideLength") + $" {input.BackSide.Length}" + callContext.Localized.GetLocalized("MustBeBetween") + $" {minBackSideLength} " + callContext.Localized.GetLocalized("And") + $" {maxBackSideLength}");
        if (input.BackSideImageList.Count() > maxImageCountPerSide)
            throw new RequestInputException(callContext.Localized.GetLocalized("InvalidBackSideImageCount") + $" {input.BackSideImageList.Count()}" + callContext.Localized.GetLocalized("MustBeNotBeAvove") + $" {maxImageCountPerSide}");

        if (input.AdditionalInfo != input.AdditionalInfo.Trim())
            throw new InvalidOperationException("Invalid additional info: not trimmed");
        if (input.AdditionalInfo.Length is < minAdditionalInfoLength or > maxAdditionalInfoLength)
            throw new RequestInputException(callContext.Localized.GetLocalized("InvalidAdditionalInfoLength") + $" {input.AdditionalInfo.Length}" + callContext.Localized.GetLocalized("MustBeBetween") + $" {minAdditionalInfoLength} " + callContext.Localized.GetLocalized("And") + $" {maxAdditionalInfoLength}");
        if (input.AdditionalInfoImageList.Count() > maxImageCountPerSide)
            throw new RequestInputException(callContext.Localized.GetLocalized("InvalidAdditionalInfoImageCount") + $" {input.AdditionalInfoImageList.Count()}" + callContext.Localized.GetLocalized("MustBeNotBeAvove") + $" {maxImageCountPerSide}");

        if (input.References != input.References.Trim())
            throw new InvalidOperationException("Invalid References: not trimmed");
        if (input.References.Length is < MinReferencesLength or > MaxReferencesLength)
            throw new RequestInputException(callContext.Localized.GetLocalized("InvalidReferencesLength") + $" {input.References.Length}" + callContext.Localized.GetLocalized("MustBeBetween") + $" {MinReferencesLength} " + callContext.Localized.GetLocalized("And") + $" {MaxReferencesLength}");

        if (input.VersionDescription != input.VersionDescription.Trim())
            throw new InvalidOperationException(ExceptionMesg_VersionDescriptionNotTrimmed);
        if (input.VersionDescription.Length is < MinVersionDescriptionLength or > MaxVersionDescriptionLength)
            throw new RequestInputException(callContext.Localized.GetLocalized("InvalidVersionDescriptionLength") + $" {input.VersionDescription.Length}" + callContext.Localized.GetLocalized("MustBeBetween") + $" {MinVersionDescriptionLength} " + callContext.Localized.GetLocalized("And") + $" {MaxVersionDescriptionLength}");

        var unionedImageLists = input.FrontSideImageList.Concat(input.BackSideImageList).Concat(input.AdditionalInfoImageList);
        if (unionedImageLists.GroupBy(guid => guid).Any(guid => guid.Count() > 1))
            throw new RequestInputException(callContext.Localized.GetLocalized("ImageDuplicated"));

        if (QueryValidationHelper.IsReservedGuid(input.LanguageId))
            throw new RequestInputException(callContext.Localized.GetLocalized("InvalidInputLanguage"));

        if (!CardVisibilityHelper.CardIsVisibleToUser(input.VersionCreatorId, input.UsersWithVisibility))
            throw new InvalidOperationException(callContext.Localized.GetLocalized("OwnerMustHaveVisibility"));

        if (input.Tags.Any(tagId => QueryValidationHelper.TagIsPerso(tagId, callContext.DbContext)) && (input.UsersWithVisibility.Count() != 1))
            throw new PersoTagAllowedOnlyOnPrivateCardsException(callContext.Localized.GetLocalized("PersoTagAllowedOnlyOnPrivateCards"));
    }
}
