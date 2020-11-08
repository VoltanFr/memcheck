using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MemCheck.Application
{
    public sealed class OldSearchCards
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public OldSearchCards(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public SearchResult Run(SearchRequest request, Guid currentUserId)
        {
            var allCards = dbContext.Cards.Include(card => card.TagsInCards).Include(card => card.VersionCreator).Include(card => card.CardLanguage).Include(card => card.UsersWithView);

            var cardsViewableByUser = allCards.Where(
                card =>
                (card.VersionCreator.Id == currentUserId)
                || (!card.UsersWithView.Any())
                || (card.UsersWithView.Where(userWithView => userWithView.UserId == currentUserId).Any())
                );

            var cardsInDeckToExclude = dbContext.CardsInDecks.Where(cardInDeck => cardInDeck.DeckId == request.DeckToExclude).Select(cardInDeck => cardInDeck.CardId);
            var cardsFilteredWithDeck = cardsViewableByUser.Where(card => !cardsInDeckToExclude.Contains(card.Id));

            //var cardsFilteredWithTags = cardsFilteredWithDeck.Where(card => card.TagsInCards.Select(tagInCard => tagInCard.TagId).Intersect(request.RequiredTags).Count() == request.RequiredTags.Count());
            var cardsFilteredWithTags = cardsFilteredWithDeck;
            foreach (var tag in request.RequiredTags)   //I tried to do better with an intersect between the two sets, but that failed
                cardsFilteredWithTags = cardsFilteredWithTags.Where(card => card.TagsInCards.Where(tagInCard => tagInCard.TagId == tag.TagId).Count() > 0);

            var totalNbCards = cardsFilteredWithTags.Count();
            var totalPageCount = totalNbCards / request.pageSize + 1;

            var pageCards = cardsFilteredWithTags.Skip((request.pageNo - 1) * request.pageSize).Take(request.pageSize);

            var resultCards = pageCards.Select(card => new SearchResultCard(
                   card.Id,
                   card.FrontSide,
                   card.BackSide,
                   card.AdditionalInfo,
                   card.VersionCreator.UserName,
                   card.CardLanguage,
                   card.TagsInCards.Select(tagInCard => tagInCard.Tag.Name),
                   card.InitialCreationUtcDate,
                   card.VersionUtcDate,
                   card.UsersWithView.Select(userWithView => userWithView.User.UserName)
                   ));

            return new SearchResult(totalNbCards, totalPageCount, resultCards);

        }
        public sealed class SearchRequest
        {
            public IEnumerable<GetAllAvailableTags.ViewModel> RequiredTags { get; set; } = null!;
            public Guid DeckToExclude { get; set; }
            public int pageNo { get; set; }
            public int pageSize { get; set; }
        }
        public sealed class SearchResult
        {
            public SearchResult(int totalNbCards, int totalPageCount, IEnumerable<SearchResultCard> cards)
            {
                TotalNbCards = totalNbCards;
                PageCount = totalPageCount;
                Cards = cards;
            }
            public int TotalNbCards { get; }
            public int PageCount { get; }
            public IEnumerable<SearchResultCard> Cards { get; }
        }
        public sealed class SearchResultCard
        {
            public SearchResultCard(Guid cardId, string frontSide, string backSide, string additionalInfo, string ownerDisplayName,
                CardLanguage language, IEnumerable<string> tags, DateTime creationUtcDate, DateTime lastEditionUtcDate, IEnumerable<string> usersWithView)
            {
                DateServices.CheckUTC(creationUtcDate);
                DateServices.CheckUTC(lastEditionUtcDate);
                CardId = cardId;
                FrontSide = frontSide;
                BackSide = backSide;
                OwnerDisplayName = ownerDisplayName;
                Tags = tags;
                Language = language;
                AdditionalInfo = additionalInfo;
                CreationUtcDate = creationUtcDate;
                LastEditionUtcDate = lastEditionUtcDate;
                UsersWithView = usersWithView;
            }
            public Guid CardId { get; }
            public string FrontSide { get; }
            public string BackSide { get; }
            public string AdditionalInfo { get; }
            public string OwnerDisplayName { get; }
            public CardLanguage Language { get; }
            public IEnumerable<string> Tags { get; }
            public DateTime CreationUtcDate { get; }
            public DateTime LastEditionUtcDate { get; }
            public IEnumerable<string> UsersWithView { get; }
        }
    }
}
