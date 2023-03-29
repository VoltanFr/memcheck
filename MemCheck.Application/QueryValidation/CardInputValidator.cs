using MemCheck.Application.Cards;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.QueryValidation;

internal static class CardInputValidator
{
    public const int MinFrontSideLength = 3;
    public const int MaxFrontSideLength = 1000;
    public const int MinBackSideLength = 1;    //A digit may be ok
    public const int MaxBackSideLength = 2000;
    public const int MinAdditionalInfoLength = 0;
    public const int MaxAdditionalInfoLength = 10000;
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
        if (input.FrontSide.Length is < MinFrontSideLength or > MaxFrontSideLength)
            throw new RequestInputException(callContext.Localized.GetLocalized("InvalidFrontSideLength") + $" {input.FrontSide.Length}" + callContext.Localized.GetLocalized("MustBeBetween") + $" {MinFrontSideLength} " + callContext.Localized.GetLocalized("And") + $" {MaxFrontSideLength}");

        if (input.BackSide != input.BackSide.Trim())
            throw new InvalidOperationException("Invalid back side: not trimmed");
        if (input.BackSide.Length is < MinBackSideLength or > MaxBackSideLength)
            throw new RequestInputException(callContext.Localized.GetLocalized("InvalidBackSideLength") + $" {input.BackSide.Length}" + callContext.Localized.GetLocalized("MustBeBetween") + $" {MinBackSideLength} " + callContext.Localized.GetLocalized("And") + $" {MaxBackSideLength}");

        if (input.AdditionalInfo != input.AdditionalInfo.Trim())
            throw new InvalidOperationException("Invalid additional info: not trimmed");
        if (input.AdditionalInfo.Length is < MinAdditionalInfoLength or > MaxAdditionalInfoLength)
            throw new RequestInputException(callContext.Localized.GetLocalized("InvalidAdditionalInfoLength") + $" {input.AdditionalInfo.Length}" + callContext.Localized.GetLocalized("MustBeBetween") + $" {MinAdditionalInfoLength} " + callContext.Localized.GetLocalized("And") + $" {MaxAdditionalInfoLength}");

        if (input.References != input.References.Trim())
            throw new InvalidOperationException("Invalid References: not trimmed");
        if (input.References.Length is < MinReferencesLength or > MaxReferencesLength)
            throw new RequestInputException(callContext.Localized.GetLocalized("InvalidReferencesLength") + $" {input.References.Length}" + callContext.Localized.GetLocalized("MustBeBetween") + $" {MinReferencesLength} " + callContext.Localized.GetLocalized("And") + $" {MaxReferencesLength}");

        if (input.VersionDescription != input.VersionDescription.Trim())
            throw new InvalidOperationException(ExceptionMesg_VersionDescriptionNotTrimmed);
        if (input.VersionDescription.Length is < MinVersionDescriptionLength or > MaxVersionDescriptionLength)
            throw new RequestInputException(callContext.Localized.GetLocalized("InvalidVersionDescriptionLength") + $" {input.VersionDescription.Length}" + callContext.Localized.GetLocalized("MustBeBetween") + $" {MinVersionDescriptionLength} " + callContext.Localized.GetLocalized("And") + $" {MaxVersionDescriptionLength}");

        if (QueryValidationHelper.IsReservedGuid(input.LanguageId))
            throw new RequestInputException(callContext.Localized.GetLocalized("InvalidInputLanguage"));

        if (!CardVisibilityHelper.CardIsVisibleToUser(input.VersionCreatorId, input.UsersWithVisibility))
            throw new InvalidOperationException(callContext.Localized.GetLocalized("OwnerMustHaveVisibility"));

        if (input.Tags.Any(tagId => QueryValidationHelper.TagIsPerso(tagId, callContext.DbContext)) && (input.UsersWithVisibility.Count() != 1))
            throw new PersoTagAllowedOnlyOnPrivateCardsException(callContext.Localized.GetLocalized("PersoTagAllowedOnlyOnPrivateCards"));
    }
}
