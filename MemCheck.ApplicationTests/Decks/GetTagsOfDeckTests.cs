﻿using MemCheck.Application.Helpers;
using MemCheck.Application.QueryValidation;
using MemCheck.Basics;
using MemCheck.Database;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Decks;

[TestClass()]
public class GetTagsOfDeckTests
{
    [TestMethod()]
    public async Task UserNotLoggedIn()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<NonexistentUserException>(async () => await new GetTagsOfDeck(dbContext).RunAsync(new GetTagsOfDeck.Request(Guid.Empty, deck)));
    }
    [TestMethod()]
    public async Task UserDoesNotExist()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);

        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<NonexistentUserException>(async () => await new GetTagsOfDeck(dbContext).RunAsync(new GetTagsOfDeck.Request(Guid.NewGuid(), deck)));
    }
    [TestMethod()]
    public async Task UserNotOwner()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user);
        var otherUser = await UserHelper.CreateInDbAsync(db);
        using var dbContext = new MemCheckDbContext(db);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new GetTagsOfDeck(dbContext).RunAsync(new GetTagsOfDeck.Request(otherUser, deck)));
    }
    [TestMethod()]
    public async Task EmptyDeck()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user.Id);
        var tag = await TagHelper.CreateAsync(db, user);
        await CardHelper.CreateAsync(db, user.Id, tagIds: tag.AsArray());

        using var dbContext = new MemCheckDbContext(db);
        var request = new GetTagsOfDeck.Request(user.Id, deck);
        var result = await new GetTagsOfDeck(dbContext).RunAsync(request);
        Assert.IsFalse(result.Any());
    }
    [TestMethod()]
    public async Task OneTag()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user.Id);
        var tag = await TagHelper.CreateAsync(db, user);
        var card = await CardHelper.CreateAsync(db, user.Id, tagIds: tag.AsArray());
        await DeckHelper.AddCardAsync(db, deck, card.Id);

        using var dbContext = new MemCheckDbContext(db);
        var request = new GetTagsOfDeck.Request(user.Id, deck);
        var result = await new GetTagsOfDeck(dbContext).RunAsync(request);
        Assert.AreEqual(tag, result.Single().TagId);
    }
    [TestMethod()]
    public async Task Complex()
    {
        var db = DbHelper.GetEmptyTestDB();
        var user = await UserHelper.CreateUserInDbAsync(db);
        var deck = await DeckHelper.CreateAsync(db, user.Id);

        var tag1 = await TagHelper.CreateAsync(db, user);
        var card1 = await CardHelper.CreateAsync(db, user.Id, tagIds: tag1.AsArray());
        await DeckHelper.AddCardAsync(db, deck, card1.Id);

        var tag2 = await TagHelper.CreateAsync(db, user);
        var card2 = await CardHelper.CreateAsync(db, user.Id, tagIds: tag2.AsArray());
        await DeckHelper.AddCardAsync(db, deck, card2.Id);

        var card3 = await CardHelper.CreateAsync(db, user.Id, tagIds: new[] { tag1, tag2 });
        await DeckHelper.AddCardAsync(db, deck, card3.Id);

        var card4 = await CardHelper.CreateAsync(db, user.Id);
        await DeckHelper.AddCardAsync(db, deck, card4.Id);

        using var dbContext = new MemCheckDbContext(db);
        var request = new GetTagsOfDeck.Request(user.Id, deck);
        var result = await new GetTagsOfDeck(dbContext).RunAsync(request);
        Assert.AreEqual(2, result.Count());
        Assert.IsTrue(result.Any(t => t.TagId == tag1));
        Assert.IsTrue(result.Any(t => t.TagId == tag2));
    }
}
