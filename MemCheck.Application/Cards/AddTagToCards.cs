using MemCheck.Application.QueryValidation;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards;

public sealed class AddTagToCards : RequestRunner<AddTagToCards.Request, AddTagToCards.Result>
{
    public AddTagToCards(CallContext callContext) : base(callContext)
    {
    }
    protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
    {
        var tagName = await DbContext.Tags.AsNoTracking().Where(tag => tag.Id == request.TagId).Select(tag => tag.Name).SingleAsync();
        foreach (var cardId in request.CardIds)
            if (!DbContext.TagsInCards.AsNoTracking().Any(tagInCard => tagInCard.CardId == cardId && tagInCard.TagId == request.TagId))
            {
                var previousVersionCreator = new PreviousCardVersionCreator(DbContext);
                await previousVersionCreator.RunAsync(cardId, request.UserId, Localized.GetLocalized("AddTag") + $" '{tagName}'");
                DbContext.TagsInCards.Add(new TagInCard() { TagId = request.TagId, CardId = cardId });
            }
        await DbContext.SaveChangesAsync();

        return new ResultWithMetrologyProperties<Result>(new Result(), ("TagId", request.TagId.ToString()), ("TagName", tagName), IntMetric("CardCount", request.CardIds.Count()));
    }
    #region Request class
    public sealed record Request(Guid UserId, Guid TagId, IEnumerable<Guid> CardIds) : IRequest
    {
        public const string ExceptionMesg_NoCard = "No card to add label to";
        public async Task CheckValidityAsync(CallContext callContext)
        {
            if (!CardIds.Any())
                throw new RequestInputException(ExceptionMesg_NoCard);
            await QueryValidationHelper.CheckUserExistsAsync(callContext.DbContext, UserId);
            await QueryValidationHelper.CheckTagExistsAsync(TagId, callContext.DbContext);
            CardVisibilityHelper.CheckUserIsAllowedToViewCards(callContext.DbContext, UserId, CardIds);
            if (QueryValidationHelper.TagIsPerso(TagId, callContext.DbContext))
            {
                try
                {
                    await CardVisibilityHelper.CheckCardsArePrivateAsync(callContext.DbContext, CardIds, UserId);
                }
                catch (RequestInputException)
                {
                    throw new PersoTagAllowedOnlyOnPrivateCardsException(callContext.Localized.GetLocalized("PersoTagAllowedOnlyOnPrivateCards"));
                }
            }
        }
    }
    public sealed record Result;
    #endregion
}
