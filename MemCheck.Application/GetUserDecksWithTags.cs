using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MemCheck.Application
{
    public sealed class GetUserDecksWithTags
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public GetUserDecksWithTags(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public IEnumerable<ResultModel> Run(Guid userId)
        {
            if (QueryValidationHelper.IsReservedGuid(userId))
                throw new RequestInputException($"Invalid user id {userId}");

            var userDecks = dbContext.Decks.AsNoTracking().Where(deck => deck.Owner.Id == userId).Select(deck => new { deckId = deck.Id, deckDescription = deck.Description }).ToList();

            var result = userDecks.Select(deck =>
                new ResultModel(deck.deckId, deck.deckDescription, new GetTagsOfDeck(dbContext).Run(deck.deckId).Select(tag => new ResultTagModel(tag.TagId, tag.TagName)))
            );

            return result.ToList();
        }
        #region Result classes
        public sealed class ResultModel
        {
            public ResultModel(Guid deckId, string description, IEnumerable<ResultTagModel> tags)
            {
                DeckId = deckId;
                Description = description;
                Tags = tags;
            }
            public Guid DeckId { get; }
            public string Description { get; }
            public IEnumerable<ResultTagModel> Tags { get; }
        }
        public sealed class ResultTagModel
        {
            public ResultTagModel(Guid tagId, string tagName)
            {
                TagId = tagId;
                TagName = tagName;
            }
            public Guid TagId { get; }
            public string TagName { get; }
            public override string ToString()
            {
                return TagId.ToString();
            }
            public override bool Equals(Object? obj)
            {
                if ((obj == null) || !GetType().Equals(obj.GetType()))
                    return false;

                ResultTagModel other = (ResultTagModel)obj;
                return TagId == other.TagId;
            }
            public override int GetHashCode()
            {
                return TagId.GetHashCode();
            }
        }
        #endregion
    }
}