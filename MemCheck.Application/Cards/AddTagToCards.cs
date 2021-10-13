using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards
{
    public sealed class AddTagToCards
    {
        #region Fields
        private readonly CallContext callContext;
        #endregion
        public AddTagToCards(CallContext callContext)
        {
            this.callContext = callContext;
        }
        public async Task RunAsync(Request request)
        {
            await request.CheckValidityAsync(callContext.DbContext);
            var tagName = await callContext.DbContext.Tags.Where(tag => tag.Id == request.TagId).Select(tag => tag.Name).SingleAsync();
            var previousVersionCreator = new PreviousVersionCreator(callContext.DbContext);
            foreach (var cardId in request.CardIds)
                if (!callContext.DbContext.TagsInCards.Any(tagInCard => tagInCard.CardId == cardId && tagInCard.TagId == request.TagId))
                {
                    var card = await previousVersionCreator.RunAsync(cardId, request.VersionCreator.Id, callContext.Localized.Get("AddTag") + $" '{tagName}'");
                    card.VersionCreator = request.VersionCreator; //A priori inutile, à confirmer
                    callContext.DbContext.TagsInCards.Add(new TagInCard() { TagId = request.TagId, CardId = cardId });
                }
            callContext.TelemetryClient.TrackEvent("AddTagToCards", ("TagId", request.TagId.ToString()), ("TagName", tagName), ("CardCount", request.CardIds.Count().ToString()));
            await callContext.DbContext.SaveChangesAsync();
        }
        #region Request class
        public sealed class Request
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
            public async Task CheckValidityAsync(MemCheckDbContext dbContext)
            {
                if (!CardIds.Any())
                    throw new RequestInputException("No card to add label to");
                if (QueryValidationHelper.IsReservedGuid(VersionCreator.Id))
                    throw new RequestInputException("Invalid user id");
                if (QueryValidationHelper.IsReservedGuid(TagId))
                    throw new RequestInputException("Reserved tag id");
                foreach (var cardId in CardIds)
                    CardVisibilityHelper.CheckUserIsAllowedToViewCards(dbContext, VersionCreator.Id, cardId);
                if (!await dbContext.Tags.Where(tag => tag.Id == TagId).AnyAsync())
                    throw new RequestInputException("Invalid tag id");
            }
        }
        #endregion
    }
}