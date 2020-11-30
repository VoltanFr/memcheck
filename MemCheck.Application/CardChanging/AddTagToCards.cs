using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.CardChanging
{
    public sealed class AddTagToCards
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        private readonly IStringLocalizer localizer;
        #endregion
        public AddTagToCards(MemCheckDbContext dbContext, IStringLocalizer localizer)
        {
            this.dbContext = dbContext;
            this.localizer = localizer;
        }
        public async Task RunAsync(Request request)
        {
            await request.CheckValidityAsync(dbContext);
            var tagName = await dbContext.Tags.Where(tag => tag.Id == request.TagId).Select(tag => tag.Name).SingleAsync();
            var previousVersionCreator = new PreviousVersionCreator(dbContext);
            foreach (var cardId in request.CardIds)
                if (!dbContext.TagsInCards.Any(tagInCard => tagInCard.CardId == cardId && tagInCard.TagId == request.TagId))
                {
                    var card = await previousVersionCreator.RunAsync(cardId, request.VersionCreator.Id, localizer["AddTag"].Value + $" '{tagName}'");
                    card.VersionCreator = request.VersionCreator; //A priori inutile, à confirmer
                    dbContext.TagsInCards.Add(new TagInCard() { TagId = request.TagId, CardId = cardId });
                }
            await dbContext.SaveChangesAsync();
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
                    await QueryValidationHelper.CheckUserIsAllowedToViewCardAsync(dbContext, VersionCreator.Id, cardId);
                if (!dbContext.Tags.Where(tag => tag.Id == TagId).Any())
                    throw new RequestInputException("Invalid tag id");
            }
        }
        #endregion
    }
}