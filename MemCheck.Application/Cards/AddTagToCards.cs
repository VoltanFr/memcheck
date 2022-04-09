using MemCheck.Application.QueryValidation;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards
{
    public sealed class AddTagToCards : RequestRunner<AddTagToCards.Request, AddTagToCards.Result>
    {
        public AddTagToCards(CallContext callContext) : base(callContext)
        {
        }
        protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
        {
            var tagName = await DbContext.Tags.Where(tag => tag.Id == request.TagId).Select(tag => tag.Name).SingleAsync();
            var previousVersionCreator = new PreviousVersionCreator(DbContext);
            foreach (var cardId in request.CardIds)
                if (!DbContext.TagsInCards.Any(tagInCard => tagInCard.CardId == cardId && tagInCard.TagId == request.TagId))
                {
                    var card = await previousVersionCreator.RunAsync(cardId, request.VersionCreator.Id, Localized.GetLocalized("AddTag") + $" '{tagName}'");
                    card.VersionCreator = request.VersionCreator; //A priori inutile, à confirmer
                    DbContext.TagsInCards.Add(new TagInCard() { TagId = request.TagId, CardId = cardId });
                }
            await DbContext.SaveChangesAsync();

            return new ResultWithMetrologyProperties<Result>(new Result(),
                ("TagId", request.TagId.ToString()),
                ("TagName", tagName),
           IntMetric("CardCount", request.CardIds.Count()));
        }
        #region Request class
        public sealed class Request : IRequest
        {
            public Request(MemCheckUser versionCreator, Guid tagId, IEnumerable<Guid> cardIds)
            {
                VersionCreator = versionCreator;
                TagId = tagId;
                CardIds = cardIds;
            }
            public MemCheckUser VersionCreator { get; }
            public Guid TagId { get; }
            public IEnumerable<Guid> CardIds { get; }
            public async Task CheckValidityAsync(CallContext callContext)
            {
                if (!CardIds.Any())
                    throw new RequestInputException("No card to add label to");
                if (QueryValidationHelper.IsReservedGuid(VersionCreator.Id))
                    throw new RequestInputException("Invalid user id");
                if (QueryValidationHelper.IsReservedGuid(TagId))
                    throw new RequestInputException("Reserved tag id");
                foreach (var cardId in CardIds)
                    CardVisibilityHelper.CheckUserIsAllowedToViewCard(callContext.DbContext, VersionCreator.Id, cardId);
                if (!await callContext.DbContext.Tags.Where(tag => tag.Id == TagId).AnyAsync())
                    throw new RequestInputException("Invalid tag id");
            }
        }
        public sealed class Result
        {
        }
        #endregion
    }
}