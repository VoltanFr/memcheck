﻿using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards
{
    public sealed class DeleteCards
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        private readonly ILocalized localizer;
        #endregion
        public DeleteCards(MemCheckDbContext dbContext, ILocalized localizer)
        {
            this.dbContext = dbContext;
            this.localizer = localizer;
        }
        public async Task RunAsync(Request request, DateTime? deletionUtcDate = null)
        {
            await request.CheckValidityAsync(dbContext, localizer);

            foreach (var cardId in request.CardIds)
            {
                var previousVersionCreator = new PreviousVersionCreator(dbContext);
                var card = await previousVersionCreator.RunAsync(cardId, request.UserId, localizer.Get("Deletion"), deletionUtcDate);
                await previousVersionCreator.RunForDeletionAsync(card, deletionUtcDate);
                dbContext.Cards.Remove(card);
            }

            await dbContext.SaveChangesAsync();
        }
        public sealed class Request
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
                var currentVersionOwnerIsUser = await dbContext.Cards
                    .Include(card => card.VersionCreator)
                    .Where(card => card.Id == cardId)
                    .Select(card => card.VersionCreator.Id == UserId).SingleAsync();

                if (!currentVersionOwnerIsUser)
                    //You are not the creator of the current version of the card with front side
                    //Vous n'êtes pas l'auteur de la version actuelle de la carte avec avant
                    throw new RequestInputException(localizer.Get("YouAreNotTheCreatorOfCurrentVersion") + await CardFrontSideInfoForExceptionMessageAsync(cardId, dbContext, localizer));

                var anyPreviousVersionHasOtherCreator = await dbContext.CardPreviousVersions
                    .Include(version => version.VersionCreator)
                    .Where(version => version.Card == cardId && version.VersionCreator.Id != UserId)
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
            public async Task CheckValidityAsync(MemCheckDbContext dbContext, ILocalized localizer)
            {
                if (CardIds.Any(cardId => QueryValidationHelper.IsReservedGuid(cardId)))
                    throw new RequestInputException($"Invalid card id");

                foreach (var cardId in CardIds)
                {
                    await CheckUsersWithCardInADeckAsync(cardId, dbContext, localizer);
                    await CheckCardVersionsCreatorsAsync(cardId, dbContext, localizer);
                }
            }
        }
    }
}
