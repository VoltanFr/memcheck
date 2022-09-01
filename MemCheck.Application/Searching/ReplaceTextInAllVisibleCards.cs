using MemCheck.Application.Cards;
using MemCheck.Application.QueryValidation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Searching;

public sealed class ReplaceTextInAllVisibleCards : RequestRunner<ReplaceTextInAllVisibleCards.Request, ReplaceTextInAllVisibleCards.Result>
{
    #region Field
    private readonly DateTime? newVersionUtcDate;
    #endregion
    public ReplaceTextInAllVisibleCards(CallContext callContext, DateTime? newVersionUtcDate = null) : base(callContext)
    {
        this.newVersionUtcDate = newVersionUtcDate;
    }
    protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
    {
        var cardIds = DbContext.Cards
            .AsNoTracking()
            .Where(card => !card.UsersWithView.Any() || card.UsersWithView.Any(userWithView => userWithView.UserId == request.UserId))
            .Where(card =>
                EF.Functions.Like(card.FrontSide, $"%{request.TextToReplace}%")
                || EF.Functions.Like(card.BackSide, $"%{request.TextToReplace}%")
                || EF.Functions.Like(card.AdditionalInfo, $"%{request.TextToReplace}%")
                || EF.Functions.Like(card.References, $"%{request.TextToReplace}%")
            )
            .Select(card => card.Id)
            .ToImmutableArray();

        var actualNewVersionUtcDate = newVersionUtcDate ?? DateTime.UtcNow;

        foreach (var cardId in cardIds)
        {
            //Reload card because in case of massive matches we don't want to hold all the cards in memory.
            //This could be improved with paging, but I don't care currently because only Azure Functions (MakeWikipediaLinksDesktop) uses this service

            var card = await DbContext.Cards
                .AsNoTracking()
                .Include(card => card.CardLanguage)
                .Include(card => card.TagsInCards)
                .Include(card => card.UsersWithView)
                .Include(card => card.Images)
                .SingleAsync(card => card.Id == cardId);

            var updateRequest = new UpdateCard.Request(
                cardId,
                request.UserId,
                card.FrontSide.Replace(request.TextToReplace, request.ReplacementText, StringComparison.OrdinalIgnoreCase),
                card.BackSide.Replace(request.TextToReplace, request.ReplacementText, StringComparison.OrdinalIgnoreCase),
                card.AdditionalInfo.Replace(request.TextToReplace, request.ReplacementText, StringComparison.OrdinalIgnoreCase),
                card.References.Replace(request.TextToReplace, request.ReplacementText, StringComparison.OrdinalIgnoreCase),
                card.CardLanguage.Id,
                card.TagsInCards.Select(t => t.TagId),
                card.UsersWithView.Select(uwv => uwv.UserId),
                request.VersionDescription);

            await new UpdateCard(CallContext, newVersionUtcDate).RunAsync(updateRequest);
        }

        var result = new Result(cardIds);
        return new ResultWithMetrologyProperties<Result>(result,
            ("TextToReplace", request.TextToReplace),
            ("ReplacementText", request.ReplacementText),
            ("VersionDescription", request.VersionDescription),
            IntMetric("ModifiedCardCount", cardIds.Length));
    }
    #region Request and result classes
    public sealed record Request(Guid UserId, string TextToReplace, string ReplacementText, string VersionDescription) : IRequest
    {
        public const int MinTextToReplaceLength = 10; //No specific reason for this value, that's just what I need for MakeWikipediaLinksDesktop
        public const string ExceptionMesgPrefix_TextToReplaceTooShort = "Text to replace is less than";
        public const string ExceptionMesgPrefix_InvalidVersionDescriptionLength = "Invalid VersionDescription length:";
        public async Task CheckValidityAsync(CallContext callContext)
        {
            await QueryValidationHelper.CheckUserExistsAsync(callContext.DbContext, UserId);

            if (TextToReplace.Length < MinTextToReplaceLength)
                throw new RequestInputException($"{ExceptionMesgPrefix_TextToReplaceTooShort} {MinTextToReplaceLength} chars long: '{TextToReplace}'");
            if (VersionDescription != VersionDescription.Trim())
                throw new InvalidOperationException(CardInputValidator.ExceptionMesg_VersionDescriptionNotTrimmed);
            if (VersionDescription.Length is < CardInputValidator.MinVersionDescriptionLength or > CardInputValidator.MaxVersionDescriptionLength)
                throw new RequestInputException($"{ExceptionMesgPrefix_InvalidVersionDescriptionLength} {VersionDescription.Length} (must be between {CardInputValidator.MinVersionDescriptionLength} and {CardInputValidator.MaxVersionDescriptionLength}");
        }
    }
    public sealed record Result(ImmutableArray<Guid> ChangedCardGuids);
    #endregion
}
