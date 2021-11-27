using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards
{
    public sealed class DeleteCards : RequestRunner<DeleteCards.Request, DeleteCards.Result>
    {
        #region Fields
        private readonly DateTime? deletionUtcDate;
        #endregion
        public DeleteCards(CallContext callContext, DateTime? deletionUtcDate = null) : base(callContext)
        {
            this.deletionUtcDate = deletionUtcDate;
        }
        protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
        {
            foreach (var cardId in request.CardIds)
            {
                var previousVersionCreator = new PreviousVersionCreator(DbContext);
                var card = await previousVersionCreator.RunAsync(cardId, request.UserId, Localized.Get("Deletion"), deletionUtcDate);
                await previousVersionCreator.RunForDeletionAsync(card, deletionUtcDate);
                DbContext.Cards.Remove(card);
            }
            await DbContext.SaveChangesAsync();
            return new ResultWithMetrologyProperties<Result>(new Result(), ("CardCount", request.CardIds.Count().ToString()));
        }
        #region Request & Result
        public sealed class Request : IRequest
        {
            #region Private methods
            private static async Task<string> CardFrontSideInfoForExceptionMessageAsync(Guid cardId, MemCheckDbContext dbContext, ILocalized localizer)
            {
                var cardFrontSide = await dbContext.Cards.Where(card => card.Id == cardId).Select(card => card.FrontSide).SingleAsync();
                if (cardFrontSide.Length < 100)
                    return $" '{cardFrontSide}'";
                return $"{localizer.Get("StartingWith")} '{cardFrontSide.Substring(0, 100)}'";
            }
            private async Task CheckUsersWithCardInADeckAsync(Guid cardId, MemCheckDbContext dbContext, ILocalized localizer)
            {
                var decksWithOtherOwner = dbContext.CardsInDecks
                    .Include(cardInDeck => cardInDeck.Deck)
                    .ThenInclude(deck => deck.Owner)
                    .Where(cardInDeck => cardInDeck.CardId == cardId && cardInDeck.Deck.Owner.Id != UserId);

                var count = await decksWithOtherOwner.CountAsync();

                if (count > 0)
                {
                    var msg = count == 1 ? localizer.Get("OneUserHasCardWithFrontSide") : (count.ToString() + localizer.Get("UsersHaveCardWithFrontSide"));
                    msg += await CardFrontSideInfoForExceptionMessageAsync(cardId, dbContext, localizer);
                    throw new RequestInputException(msg);
                }
            }
            private async Task CheckCardVersionsCreatorsAsync(Guid cardId, MemCheckDbContext dbContext, ILocalized localizer)
            {
                var currentVersionCreator = await dbContext.Cards
                    .Include(card => card.VersionCreator)
                    .Where(card => card.Id == cardId)
                    .Select(card => card.VersionCreator)
                    .SingleAsync();

                if (currentVersionCreator.Id != UserId && currentVersionCreator.DeletionDate == null)
                    //You are not the creator of the current version of the card with front side
                    //Vous n'êtes pas l'auteur de la version actuelle de la carte avec avant
                    throw new RequestInputException(localizer.Get("YouAreNotTheCreatorOfCurrentVersion") + await CardFrontSideInfoForExceptionMessageAsync(cardId, dbContext, localizer));

                var anyPreviousVersionHasOtherCreator = await dbContext.CardPreviousVersions
                    .Include(version => version.VersionCreator)
                    .Where(version => version.Card == cardId && version.VersionCreator.Id != UserId && version.VersionCreator.DeletionDate == null)
                    .AnyAsync();

                if (anyPreviousVersionHasOtherCreator)
                    //You are not the creator of all the previous versions of the card with front side
                    //Vous n'êtes pas l'auteur de toutes les versions précédentes de la carte avec avant
                    throw new RequestInputException(localizer.Get("YouAreNotTheCreatorOfAllPreviousVersions") + await CardFrontSideInfoForExceptionMessageAsync(cardId, dbContext, localizer));
            }
            #endregion
            public Request(Guid userId, IEnumerable<Guid> cardIds)
            {
                UserId = userId;
                CardIds = cardIds;
            }
            public Guid UserId { get; }
            public IEnumerable<Guid> CardIds { get; }
            public async Task CheckValidityAsync(CallContext callContext)
            {
                await QueryValidationHelper.CheckUserExistsAsync(callContext.DbContext, UserId);
                if (CardIds.Any(cardId => QueryValidationHelper.IsReservedGuid(cardId)))
                    throw new RequestInputException($"Invalid card id");
                CardVisibilityHelper.CheckUserIsAllowedToViewCards(callContext.DbContext, UserId, CardIds.ToArray());
                foreach (var cardId in CardIds)
                {
                    await CheckUsersWithCardInADeckAsync(cardId, callContext.DbContext, callContext.Localized);
                    await CheckCardVersionsCreatorsAsync(cardId, callContext.DbContext, callContext.Localized);
                }
            }
        }
        public sealed record Result
        {
        }
        #endregion
    }
}
