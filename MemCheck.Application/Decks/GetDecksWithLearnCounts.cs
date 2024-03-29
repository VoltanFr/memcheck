﻿using MemCheck.Application.QueryValidation;
using MemCheck.Basics;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Decks;

public sealed class GetDecksWithLearnCounts : RequestRunner<GetDecksWithLearnCounts.Request, ImmutableArray<GetDecksWithLearnCounts.Result>>
{
    #region Fields
    private static readonly TimeSpan oneHour = TimeSpan.FromHours(1);
    private static readonly TimeSpan twentyFiveHours = TimeSpan.FromHours(25);
    private static readonly TimeSpan fourDays = TimeSpan.FromDays(4);
    private readonly DateTime? runDate;
    #endregion
    #region Private methods
    private static async Task<Result> GetDeckAsync(MemCheckDbContext dbContext, Guid deckId, string description, DateTime runDate)
    {
        var allCards = await dbContext.CardsInDecks
            .AsNoTracking()
            .Where(cardInDeck => cardInDeck.DeckId == deckId)
            .Select(cardInDeck => new { cardInDeck.CurrentHeap, cardInDeck.ExpiryUtcTime })
            .ToImmutableArrayAsync();

        var unknownCardCount = 0;
        var expiredCardCount = 0;
        var nextExpiryUTCDate = DateTime.MaxValue;
        var expiringNextHourCount = 0;
        var expiringFollowing24hCount = 0;
        var expiringFollowing3DaysCount = 0;

        foreach (var card in allCards)
            if (card.CurrentHeap == CardInDeck.UnknownHeap)
                unknownCardCount++;
            else
            {
                if (card.ExpiryUtcTime <= runDate)
                    expiredCardCount++;
                else
                {
                    var distanceToNow = card.ExpiryUtcTime - runDate;
                    if (distanceToNow <= oneHour)
                        expiringNextHourCount++;
                    else
                    {
                        if (distanceToNow <= twentyFiveHours)
                            expiringFollowing24hCount++;
                        else
                        {
                            if (distanceToNow <= fourDays)
                                expiringFollowing3DaysCount++;
                        }
                    }
                    if (card.ExpiryUtcTime < nextExpiryUTCDate)
                        nextExpiryUTCDate = card.ExpiryUtcTime;
                }
            }
        return new Result(deckId, description, unknownCardCount, expiredCardCount, allCards.Length, expiringNextHourCount, expiringFollowing24hCount, expiringFollowing3DaysCount, nextExpiryUTCDate);
    }
    #endregion
    public GetDecksWithLearnCounts(CallContext callContext, DateTime? runDate = null) : base(callContext)
    {
        this.runDate = runDate;
    }
    protected override async Task<ResultWithMetrologyProperties<ImmutableArray<Result>>> DoRunAsync(Request request)
    {
        var now = runDate == null ? DateTime.UtcNow : runDate;
        var decks = await DbContext.Decks
            .AsNoTracking()
            .Where(deck => deck.Owner.Id == request.UserId)
            .Select(deck => new { deck.Id, deck.Description })
            .ToImmutableArrayAsync();
        var result = new List<Result>();
        foreach (var deck in decks)
            result.Add(await GetDeckAsync(DbContext, deck.Id, deck.Description, now.Value));
        return new ResultWithMetrologyProperties<ImmutableArray<Result>>(result.ToImmutableArray(), IntMetric("DeckCount", result.Count));
    }
    #region Request & Result
    public sealed record Request(Guid UserId) : IRequest
    {
        public async Task CheckValidityAsync(CallContext callContext)
        {
            await QueryValidationHelper.CheckUserExistsAsync(callContext.DbContext, UserId);
        }
    }
    public sealed record Result(
        Guid Id,
        string Description,
        int UnknownCardCount,
        int ExpiredCardCount,
        int CardCount,
        int ExpiringNextHourCount,
        int ExpiringFollowing24hCount,
        int ExpiringFollowing3DaysCount,
        DateTime NextExpiryUTCDate); //NextExpiryUTCDate is DateTime.MaxValue if all cards are unknown
    #endregion
}
