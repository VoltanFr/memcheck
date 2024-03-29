﻿using MemCheck.Application;
using MemCheck.Application.Cards;
using MemCheck.Application.Decks;
using MemCheck.Application.History;
using MemCheck.Application.QueryValidation;
using MemCheck.Application.Ratings;
using MemCheck.Application.Tags;
using MemCheck.Application.Users;
using MemCheck.Basics;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemCheck.WebUI.Controllers;

[Route("[controller]"), Authorize]
public class AuthoringController : MemCheckController
{
    #region Fields
    private readonly CallContext callContext;
    private readonly MemCheckUserManager userManager;
    #endregion
    public AuthoringController(MemCheckDbContext dbContext, MemCheckUserManager userManager, IStringLocalizer<AuthoringController> localizer, TelemetryClient telemetryClient) : base(localizer)
    {
        callContext = new CallContext(dbContext, new MemCheckTelemetryClient(telemetryClient), this, new ProdRoleChecker(userManager));
        this.userManager = userManager;
    }
    #region GetUsers, returns IEnumerable<GetUsersViewModel>
    [HttpGet("GetUsers")]
    public async Task<IActionResult> GetUsersAsync()
    {
        var result = await new GetUsers(callContext).RunAsync(new GetUsers.Request());
        return Ok(result.Select(user => new GetUsersViewModel(user.UserId, user.UserName)));
    }
    public sealed class GetUsersViewModel
    {
        public GetUsersViewModel(Guid userId, string userName)
        {
            UserId = userId;
            UserName = userName;
        }
        public Guid UserId { get; }
        public string UserName { get; }
    }
    #endregion
    #region GetCurrentUser
    [HttpGet("GetCurrentUser")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var user = await userManager.GetExistingUserAsync(HttpContext.User);
        return Ok(new GetCurrentUserViewModel(user, callContext.DbContext));
    }
    public sealed class GetCurrentUserViewModel
    {
        public GetCurrentUserViewModel(MemCheckUser user, MemCheckDbContext dbContext)
        {
            UserId = user.Id;
            UserName = user.GetUserName();
            var cardLanguage = user.PreferredCardCreationLanguage ?? dbContext.CardLanguages.OrderBy(lang => lang.Name).First();
            PreferredCardCreationLanguageId = cardLanguage.Id;
        }
        public Guid UserId { get; }
        public string UserName { get; }
        public Guid PreferredCardCreationLanguageId { get; }
    }
    #endregion
    #region PostCardOfUser
    [HttpPost("CardsOfUser")]
    public async Task<IActionResult> PostCardOfUser([FromBody] PostCardOfUserRequest card)
    {
        CheckBodyParameter(card);
        var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
        var versionDescription = GetLocalized("InitialCardVersionCreation");
        var request = new CreateCard.Request(userId, card.FrontSide!, card.BackSide!, card.AdditionalInfo, card.References, card.LanguageId, card.Tags, card.UsersWithVisibility, versionDescription);
        var cardId = (await new CreateCard(callContext).RunAsync(request)).CardId;
        if (card.AddToDeck != Guid.Empty)
            await new AddCardsInDeck(callContext).RunAsync(new AddCardsInDeck.Request(userId, card.AddToDeck, cardId.AsArray()));
        return ControllerResultWithToast.Success(GetLocalized("CardSavedOk"), this);
    }
    public sealed class PostCardOfUserRequest
    {
        public string FrontSide { get; set; } = null!;
        public string BackSide { get; set; } = null!;
        public string AdditionalInfo { get; set; } = null!;
        public string References { get; set; } = null!;
        public Guid LanguageId { get; set; }
        public Guid AddToDeck { get; set; }
        public IEnumerable<Guid> Tags { get; set; } = null!;
        public IEnumerable<Guid> UsersWithVisibility { get; set; } = null!;
    }
    #endregion
    #region UpdateCard
    [HttpPut("UpdateCard/{cardId}")]
    public async Task<IActionResult> UpdateCard(Guid cardId, [FromBody] UpdateCardRequest card)
    {
        CheckBodyParameter(card);
        var user = await userManager.GetExistingUserAsync(HttpContext.User);
        var request = new UpdateCard.Request(cardId, user.Id, card.FrontSide, card.BackSide, card.AdditionalInfo, card.References, card.LanguageId, card.Tags, card.UsersWithVisibility, card.VersionDescription);
        await new UpdateCard(callContext).RunAsync(request);
        return ControllerResultWithToast.Success(GetLocalized("CardSavedOk"), this);
    }
    public sealed class UpdateCardRequest
    {
        public string FrontSide { get; set; } = null!;
        public string BackSide { get; set; } = null!;
        public string AdditionalInfo { get; set; } = null!;
        public string References { get; set; } = null!;
        public Guid LanguageId { get; set; }
        public Guid AddToDeck { get; set; }
        public IEnumerable<Guid> Tags { get; set; } = null!;
        public IEnumerable<Guid> UsersWithVisibility { get; set; } = null!;
        public string VersionDescription { get; set; } = null!;
    }
    #endregion
    #region GetAllAvailableTags
    [HttpGet("AllAvailableTags")]
    public async Task<IActionResult> GetAllAvailableTagsAsync()
    {
        var result = await new GetAllTags(callContext).RunAsync(new GetAllTags.Request(GetAllTags.Request.MaxPageSize, 1, ""));
        return Ok(result.Tags.Select(tag => new GetAllAvailableTagsViewModel(tag.TagId, tag.TagName)));
    }
    public sealed class GetAllAvailableTagsViewModel
    {
        public GetAllAvailableTagsViewModel(Guid tagId, string tagName)
        {
            TagId = tagId;
            TagName = tagName;
        }
        public Guid TagId { get; }
        public string TagName { get; }
    }
    #endregion
    #region GetCardForEdit
    [HttpGet("GetCardForEdit/{cardId}")]
    public async Task<IActionResult> GetCardForEdit(Guid cardId)
    {
        var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
        var result = await new GetCardForEdit(callContext).RunAsync(new GetCardForEdit.Request(userId, cardId));
        return Ok(new GetCardForEditViewModel(result, this));
    }
    #region Request and view model classes
    internal sealed class GetCardForEditViewModel
    {
        public GetCardForEditViewModel(GetCardForEdit.ResultModel applicationResult, ILocalized localizer)
        {
            FrontSide = applicationResult.FrontSide;
            BackSide = applicationResult.BackSide;
            AdditionalInfo = applicationResult.AdditionalInfo;
            References = applicationResult.References;
            LanguageId = applicationResult.LanguageId;
            Tags = applicationResult.Tags.Select(tag => new GetAllAvailableTagsViewModel(tag.TagId, tag.TagName)).ToImmutableArray();
            UsersWithVisibility = applicationResult.UsersWithVisibility.Select(user => new GetUsersViewModel(user.UserId, user.UserName)).ToImmutableArray();
            PossibleTargetDecksForAdd = applicationResult.PossibleTargetDecksForAdd.Select(deck => new GetCardForEditDeckModel(deck.DeckId, deck.DeckName)).ToImmutableArray();
            CreationUtcDate = applicationResult.FirstVersionUtcDate;
            LastChangeUtcDate = applicationResult.LastVersionUtcDate;
            InfoAboutUsage = applicationResult.UsersOwningDeckIncluding.Any() ? localizer.GetLocalized("AppearsInDecksOf") + ' ' + string.Join(',', applicationResult.UsersOwningDeckIncluding) : localizer.GetLocalized("NotIncludedInAnyDeck");
            CurrentUserRating = applicationResult.UserRating;
            AverageRating = Math.Round(applicationResult.AverageRating, 1);
            CountOfUserRatings = applicationResult.CountOfUserRatings;
            LatestDiscussionEntryCreationUtcDate = applicationResult.LatestDiscussionEntryCreationUtcDate ?? DateTime.MinValue;
        }
        public string FrontSide { get; }
        public string BackSide { get; }
        public string AdditionalInfo { get; }
        public string References { get; }
        public Guid LanguageId { get; }
        public ImmutableArray<GetAllAvailableTagsViewModel> Tags { get; }
        public ImmutableArray<GetUsersViewModel> UsersWithVisibility { get; }
        public ImmutableArray<GetCardForEditDeckModel> PossibleTargetDecksForAdd { get; }
        public DateTime CreationUtcDate { get; }
        public DateTime LastChangeUtcDate { get; }
        public string InfoAboutUsage { get; }
        public int CurrentUserRating { get; }
        public double AverageRating { get; }
        public int CountOfUserRatings { get; }
        public DateTime LatestDiscussionEntryCreationUtcDate { get; }
    }
    public sealed class GetCardForEditDeckModel
    {
        public GetCardForEditDeckModel(Guid deckId, string deckName)
        {
            DeckId = deckId;
            DeckName = deckName;
        }
        public Guid DeckId { get; }
        public string DeckName { get; }
    }
    #endregion
    #endregion
    #region DecksOfUser, returns IEnumerable<GetUsersViewModel>
    [HttpGet("DecksOfUser")]
    public async Task<IActionResult> DecksOfUser()
    {
        var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
        var result = await new GetUserDecks(callContext).RunAsync(new GetUserDecks.Request(userId));
        return Ok(result.Select(deck => new DecksOfUserViewModel(deck.DeckId, deck.Description)));
    }
    public sealed class DecksOfUserViewModel
    {
        public DecksOfUserViewModel(Guid deckId, string deckName)
        {
            DeckId = deckId;
            DeckName = deckName;
        }
        public Guid DeckId { get; }
        public string DeckName { get; }
    }
    #endregion
    #region GetImageInfo
    [HttpPost("GetImageInfo")]
    public async Task<IActionResult> GetImageInfo([FromBody] GetImageInfoRequest request)
    {
        CheckBodyParameter(request);
        var appResult = await new Application.Images.GetImageInfoFromName(callContext).RunAsync(new Application.Images.GetImageInfoFromName.Request(request.ImageName.Trim()));
        return Ok(new GetImageInfoViewModel(appResult.Id, request.ImageName, appResult.Source));
    }
    #region Request and view model classes
    public sealed class GetImageInfoRequest
    {
        public string ImageName { get; set; } = null!;
    }
    public sealed class GetImageInfoViewModel
    {
        public GetImageInfoViewModel(Guid imageId, string name, string source)
        {
            ImageId = imageId;
            Name = name;
            Source = source;
        }
        public Guid ImageId { get; }
        public string Name { get; }
        public string Source { get; }
    }
    #endregion
    #endregion
    #region CardVersions
    [HttpGet("CardVersions/{cardId}")]
    public async Task<IActionResult> CardVersions(Guid cardId)
    {
        var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
        var appResults = await new GetCardVersions(callContext).RunAsync(new GetCardVersions.Request(userId, cardId));
        var result = appResults.Select(appResult => new CardVersion(appResult, this));
        return Ok(result);
    }
    public sealed class CardVersion
    {
        public CardVersion(GetCardVersions.IResultCardVersion appResult, ILocalized localizer)
        {
            VersionId = appResult.VersionId;
            VersionUtcDate = appResult.VersionUtcDate;
            VersionCreator = appResult.VersionCreator;
            VersionDescription = appResult.VersionDescription;
            var fieldsDisplayNames = appResult.ChangedFieldNames.Select(fieldName => localizer.GetLocalized(fieldName));
            ChangedFieldList = string.Join(',', fieldsDisplayNames);
        }
        public Guid? VersionId { get; } //null if this is the current version of the card, ie not a previous version
        public DateTime VersionUtcDate { get; }
        public string VersionCreator { get; }
        public string VersionDescription { get; }
        public string ChangedFieldList { get; }
    }
    #endregion
    #region SetCardRating
    [HttpPatch("SetCardRating/{cardId}/{rating}")]
    public async Task<IActionResult> SetCardRating(Guid cardId, int rating)
    {
        var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
        var request = new SetCardRating.Request(userId, cardId, rating);
        await new SetCardRating(callContext).RunAsync(request);
        return ControllerResultWithToast.Success($"{GetLocalized("RatingSavedOk")} {rating}\u2605", this);
    }
    #endregion
    #region CardVersionDiffWithCurrent
    [HttpGet("CardSelectedVersionDiffWithCurrent/{cardId}/{selectedVersionId}")]
    public async Task<IActionResult> CardSelectedVersionDiffWithCurrent(Guid cardId, Guid selectedVersionId)
    {
        var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
        var card = await new GetCardForEdit(callContext).RunAsync(new GetCardForEdit.Request(userId, cardId));
        var selectedVersion = await new GetCardVersion(callContext).RunAsync(new GetCardVersion.Request(userId, selectedVersionId));
        var result = new CardSelectedVersionDiffWithCurrentResult(card, selectedVersion, this);
        return Ok(result);
    }
    public sealed class CardSelectedVersionDiffWithCurrentResult
    {
        #region Private methods
        private static void AddField(List<string> changedFields, List<string> unChangedFields, string fieldNameResourceId, string fieldValueInCard, string fieldValueInSelectedVersion, ILocalized localizer)
        {
            if (fieldValueInCard == fieldValueInSelectedVersion)
                unChangedFields.Add($"<strong>{localizer.GetLocalized(fieldNameResourceId)}</strong> {(fieldValueInCard.Length > 0 ? fieldValueInCard : localizer.GetLocalized("Empty"))}");
            else
            {
                var html = new StringBuilder();
                html.Append(CultureInfo.InvariantCulture, $"<strong>{localizer.GetLocalized(fieldNameResourceId)}</strong>");
                html.Append("<ul>");
                html.Append(CultureInfo.InvariantCulture, $"<li><strong>{localizer.GetLocalized("SelectedVersion")}</strong> {(fieldValueInSelectedVersion.Length > 0 ? fieldValueInSelectedVersion : localizer.GetLocalized("Empty"))}</li>");
                html.Append(CultureInfo.InvariantCulture, $"<li><strong>{localizer.GetLocalized("LastVersion")}</strong> {(fieldValueInCard.Length > 0 ? fieldValueInCard : localizer.GetLocalized("Empty"))}</li>");
                html.Append("</ul>");
                changedFields.Add(html.ToString());
            }
        }
        #endregion
        public CardSelectedVersionDiffWithCurrentResult(GetCardForEdit.ResultModel card, GetCardVersion.Result selectedVersion, ILocalized localizer)
        {
            FirstVersionUtcDate = card.FirstVersionUtcDate;
            LastVersionUtcDate = card.LastVersionUtcDate;
            LastVersionCreatorName = card.LastVersionCreatorName;
            LastVersionDescription = card.LastVersionDescription;
            InfoAboutUsage = card.UsersOwningDeckIncluding.Any() ? localizer.GetLocalized("AppearsInDecksOf") + ' ' + string.Join(',', card.UsersOwningDeckIncluding) : localizer.GetLocalized("NotIncludedInAnyDeck");
            AverageRating = card.AverageRating;
            CountOfUserRatings = card.CountOfUserRatings;
            SelectedVersionUtcDate = selectedVersion.VersionUtcDate;
            SelectedVersionDescription = selectedVersion.VersionDescription;
            SelectedVersionCreatorName = selectedVersion.CreatorName;

            var changedFields = new List<string>();
            var unChangedFields = new List<string>();
            AddField(changedFields, unChangedFields, "FrontSide", card.FrontSide, selectedVersion.FrontSide, localizer);
            AddField(changedFields, unChangedFields, "BackSide", card.BackSide, selectedVersion.BackSide, localizer);
            AddField(changedFields, unChangedFields, "AdditionalInfo", card.AdditionalInfo, selectedVersion.AdditionalInfo, localizer);
            AddField(changedFields, unChangedFields, "References", card.References, selectedVersion.References, localizer);
            AddField(changedFields, unChangedFields, "LanguageName", card.LanguageName, selectedVersion.LanguageName, localizer);

            var cardTags = card.Tags.Any() ? string.Join(",", card.Tags.Select(t => t.TagName).OrderBy(name => name)) : localizer.GetLocalized("NoneMasc");
            var versionTags = selectedVersion.Tags.Any() ? string.Join(",", selectedVersion.Tags.OrderBy(name => name)) : localizer.GetLocalized("NoneMasc");
            AddField(changedFields, unChangedFields, card.Tags.Length > 1 && selectedVersion.Tags.Count() > 1 ? "Tags" : "Tag", cardTags, versionTags, localizer);

            var cardVisibility = card.UsersWithVisibility.Any() ? string.Join(",", card.UsersWithVisibility.Select(u => u.UserName).OrderBy(name => name)) : localizer.GetLocalized("Public");
            var versionVisibility = selectedVersion.UsersWithVisibility.Any() ? string.Join(",", selectedVersion.UsersWithVisibility.OrderBy(name => name)) : localizer.GetLocalized("Public");
            AddField(changedFields, unChangedFields, "Visibility", cardVisibility, versionVisibility, localizer);

            ChangedFields = changedFields;
            UnchangedFields = unChangedFields;
        }
        public DateTime FirstVersionUtcDate { get; }
        public DateTime LastVersionUtcDate { get; }
        public string LastVersionCreatorName { get; }
        public string LastVersionDescription { get; }
        public string InfoAboutUsage { get; }
        public double AverageRating { get; }
        public int CountOfUserRatings { get; }
        public DateTime SelectedVersionUtcDate { get; }
        public string SelectedVersionDescription { get; }
        public string SelectedVersionCreatorName { get; }

        public IEnumerable<string> ChangedFields { get; }
        public IEnumerable<string> UnchangedFields { get; }
    }
    #endregion
    #region AddCardToDeck
    [HttpPatch("AddCardToDeck/{cardId}/{deckId}")]
    public async Task<IActionResult> AddCardToDeck(Guid cardId, Guid deckId)
    {
        var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
        var request = new AddCardsInDeck.Request(userId, deckId, cardId);
        await new AddCardsInDeck(callContext).RunAsync(request);
        return ControllerResultWithToast.Success(GetLocalized("CardAdded"), this);
    }
    #endregion
    #region PostDiscussionEntry
    [HttpPost("PostDiscussionEntry")]
    public async Task<IActionResult> PostDiscussionEntry([FromBody] PostDiscussionEntryRequest request)
    {
        CheckBodyParameter(request);
        var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
        var appRequest = new AddEntryToCardDiscussion.Request(userId, request.CardId, request.Text);
        var appResult = await new AddEntryToCardDiscussion(callContext).RunAsync(appRequest);
        return Ok(new PostDiscussionEntryResult(appResult.EntryCountForCard));
    }
    #region Request and result classes
    public sealed class PostDiscussionEntryRequest
    {
        public Guid CardId { get; set; }
        public string Text { get; set; } = null!;
    }
    public sealed record PostDiscussionEntryResult(int EntryCount);
    #endregion
    #endregion
    #region GetDiscussionEntries
    [HttpPost("GetDiscussionEntries")]
    public async Task<IActionResult> GetDiscussionEntries([FromBody] GetDiscussionEntriesRequest request)
    {
        var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
        var applicationRequest = new GetCardDiscussionEntries.Request(userId, request.CardId, request.PageSize, request.LastObtainedEntry);
        try
        {
            var applicationResult = await new GetCardDiscussionEntries(callContext).RunAsync(applicationRequest);
            var result = new GetDiscussionEntriesResult(applicationResult);
            return base.Ok(result);
        }
        catch (NonexistentCardException)
        {
            return NotFound();
        }
    }
    #region Request and Result classes
    public sealed class GetDiscussionEntriesRequest
    {
        public int PageSize { get; set; }
        public Guid CardId { get; set; }
        public Guid LastObtainedEntry { get; set; } // We don't receive a page index because if someone adds an entry in the meanwhile this index would be shifted, and the last obtained entry would be displayed twice
    }
    public sealed class GetDiscussionEntriesResult
    {
        public GetDiscussionEntriesResult(GetCardDiscussionEntries.Result applicationResult)
        {
            TotalEntryCount = applicationResult.TotalCount;
            Entries = applicationResult.Entries.Select(entry => new GetDiscussionEntriesResultEntry(entry));
        }
        public int TotalEntryCount { get; }
        public IEnumerable<GetDiscussionEntriesResultEntry> Entries { get; }
    }
    public sealed class GetDiscussionEntriesResultEntry
    {
        public GetDiscussionEntriesResultEntry(GetCardDiscussionEntries.ResultEntry applicationResultEntry)
        {
            EntryId = applicationResultEntry.Id;
            AuthorUserName = applicationResultEntry.Creator.GetUserName();
            Text = applicationResultEntry.Text;
            UtcDate = applicationResultEntry.CreationUtcDate;
            HasBeenEdited = applicationResultEntry.HasBeenEdited;
        }
        public Guid EntryId { get; }
        public string AuthorUserName { get; }
        public string Text { get; }
        public DateTime UtcDate { get; }
        public bool HasBeenEdited { get; }
    }
    public sealed record ResultEntry(Guid Id, MemCheckUser Creator, string Text, DateTime CreationUtcDate, bool HasBeenEdited);
    #endregion
    #endregion
}
