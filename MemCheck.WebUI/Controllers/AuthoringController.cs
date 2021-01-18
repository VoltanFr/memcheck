using MemCheck.Application;
using MemCheck.Application.CardChanging;
using MemCheck.Application.History;
using MemCheck.Application.Loading;
using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemCheck.WebUI.Controllers
{
    [Route("[controller]"), Authorize]
    public class AuthoringController : MemCheckController
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        private readonly UserManager<MemCheckUser> userManager;
        #endregion
        public AuthoringController(MemCheckDbContext dbContext, UserManager<MemCheckUser> userManager, IStringLocalizer<AuthoringController> localizer) : base(localizer)
        {
            this.dbContext = dbContext;
            this.userManager = userManager;
        }
        #region GetUsers, returns IEnumerable<GetUsersViewModel>
        [HttpGet("GetUsers")]
        public IActionResult GetUsers()
        {
            var result = new GetUsers(dbContext).Run();
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
            var user = await userManager.GetUserAsync(HttpContext.User);
            return Ok(new GetCurrentUserViewModel(user, dbContext));
        }
        public sealed class GetCurrentUserViewModel
        {
            public GetCurrentUserViewModel(MemCheckUser user, MemCheckDbContext dbContext)
            {
                UserId = user.Id;
                UserName = user.UserName;
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
            var user = await userManager.GetUserAsync(HttpContext.User);
            var versionDescription = Get("InitialCardVersionCreation");
            var request = new CreateCard.Request(user.Id, card.FrontSide!, card.FrontSideImageList, card.BackSide!, card.BackSideImageList, card.AdditionalInfo!, card.AdditionalInfoImageList, card.LanguageId, card.Tags, card.UsersWithVisibility, versionDescription);
            var cardId = await new CreateCard(dbContext).RunAsync(request, this);
            if (card.AddToDeck != Guid.Empty)
                await new AddCardInDeck(dbContext).RunAsync(card.AddToDeck, cardId);
            return ControllerResultWithToast.Success(Get("CardSavedOk"), this);
        }
        public sealed class PostCardOfUserRequest
        {
            public string FrontSide { get; set; } = null!;
            public IEnumerable<Guid> FrontSideImageList { get; set; } = null!;
            public string BackSide { get; set; } = null!;
            public IEnumerable<Guid> BackSideImageList { get; set; } = null!;
            public string AdditionalInfo { get; set; } = null!;
            public IEnumerable<Guid> AdditionalInfoImageList { get; set; } = null!;
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
            var user = await userManager.GetUserAsync(HttpContext.User);
            var request = new UpdateCard.Request(cardId, user.Id, card.FrontSide, card.FrontSideImageList, card.BackSide, card.BackSideImageList, card.AdditionalInfo, card.AdditionalInfoImageList, card.LanguageId, card.Tags, card.UsersWithVisibility, card.VersionDescription);
            await new UpdateCard(dbContext).RunAsync(request, this);
            return ControllerResultWithToast.Success(Get("CardSavedOk"), this);
        }
        public sealed class UpdateCardRequest
        {
            public string FrontSide { get; set; } = null!;
            public IEnumerable<Guid> FrontSideImageList { get; set; } = null!;
            public string BackSide { get; set; } = null!;
            public IEnumerable<Guid> BackSideImageList { get; set; } = null!;
            public string AdditionalInfo { get; set; } = null!;
            public IEnumerable<Guid> AdditionalInfoImageList { get; set; } = null!;
            public Guid LanguageId { get; set; }
            public Guid AddToDeck { get; set; }
            public IEnumerable<Guid> Tags { get; set; } = null!;
            public IEnumerable<Guid> UsersWithVisibility { get; set; } = null!;
            public string VersionDescription { get; set; } = null!;
        }
        #endregion
        #region GetAllAvailableTags
        [HttpGet("AllAvailableTags")]
        public IActionResult GetAllAvailableTags()
        {
            var result = new GetAllAvailableTags(dbContext).Run();
            return Ok(result.Select(tag => new GetAllAvailableTagsViewModel(tag.TagId, tag.Name)));
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
            var result = new GetCardForEdit(dbContext).RunAsync(new GetCardForEdit.Request(userId, cardId));
            return Ok(new GetCardForEditViewModel(await result, this));
        }
        #region Request and view model classes
        internal sealed class GetCardForEditViewModel
        {
            public GetCardForEditViewModel(GetCardForEdit.ResultModel applicationResult, ILocalized localizer)
            {
                FrontSide = applicationResult.FrontSide;
                BackSide = applicationResult.BackSide;
                AdditionalInfo = applicationResult.AdditionalInfo;
                LanguageId = applicationResult.LanguageId;
                Tags = applicationResult.Tags.Select(tag => new GetAllAvailableTagsViewModel(tag.TagId, tag.TagName));
                UsersWithVisibility = applicationResult.UsersWithVisibility.Select(user => new GetUsersViewModel(user.UserId, user.UserName));
                CreationUtcDate = applicationResult.FirstVersionUtcDate;
                LastChangeUtcDate = applicationResult.LastVersionUtcDate;
                InfoAboutUsage = applicationResult.UsersOwningDeckIncluding.Count() > 0 ? localizer.Get("AppearsInDecksOf") + ' ' + string.Join(',', applicationResult.UsersOwningDeckIncluding) : localizer.Get("NotIncludedInAnyDeck");
                Images = applicationResult.Images.Select(applicationImage => new GetCardForEditImageViewModel(applicationImage));
                CurrentUserRating = applicationResult.UserRating;
                AverageRating = Math.Round(applicationResult.AverageRating, 1);
                CountOfUserRatings = applicationResult.CountOfUserRatings;
            }
            public string FrontSide { get; }
            public string BackSide { get; }
            public string AdditionalInfo { get; }
            public Guid LanguageId { get; }
            public IEnumerable<GetAllAvailableTagsViewModel> Tags { get; }
            public IEnumerable<GetUsersViewModel> UsersWithVisibility { get; }
            public DateTime CreationUtcDate { get; }
            public DateTime LastChangeUtcDate { get; }
            public string InfoAboutUsage { get; }
            public IEnumerable<GetCardForEditImageViewModel> Images { get; }
            public int CurrentUserRating { get; }
            public double AverageRating { get; }
            public int CountOfUserRatings { get; }
        }
        public sealed class GetCardForEditImageViewModel
        {
            internal GetCardForEditImageViewModel(GetCardForEdit.ResultImageModel appResult)
            {
                ImageId = appResult.ImageId;
                Name = appResult.Name;
                Source = appResult.Source;
                CardSide = appResult.CardSide;
            }
            public Guid ImageId { get; }
            public string Name { get; }
            public string Source { get; }
            public int CardSide { get; set; }   //1 = front side ; 2 = back side ; 3 = AdditionalInfo
        }
        #endregion
        #endregion
        #region GetGuiMessages
        [HttpGet("GetGuiMessages")]
        public IActionResult GetGuiMessages()
        {
            return Ok(new GetGuiMessagesViewModel(
            Get("Success"),
            Get("Failure"),
            Get("SureCreateWithoutTag")
            ));
        }
        public sealed class GetGuiMessagesViewModel
        {
            public GetGuiMessagesViewModel(string success, string failure, string sureCreateWithoutTag)
            {
                Success = success;
                Failure = failure;
                SureCreateWithoutTag = sureCreateWithoutTag;
            }
            public string Success { get; } = null!;
            public string Failure { get; } = null!;
            public string SureCreateWithoutTag { get; } = null!;
        }
        #endregion
        #region DecksOfUser, returns IEnumerable<GetUsersViewModel>
        [HttpGet("DecksOfUser")]
        public async Task<IActionResult> DecksOfUser()
        {
            var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
            var result = new GetUserDecks(dbContext).Run(userId);
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
            var appResult = await new GetImageInfo(dbContext, this).RunAsync(request.ImageName);
            return Ok(new GetImageInfoViewModel(appResult.ImageId, appResult.Name, appResult.Source));
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
            var appResults = await new GetCardVersions(dbContext, this).RunAsync(new GetCardVersions.Request(userId, cardId));
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
                var fieldsDisplayNames = appResult.ChangedFieldNames.Select(fieldName => localizer.Get(fieldName));
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
            var user = await userManager.GetUserAsync(HttpContext.User);
            var request = new SetCardRating.Request(user, cardId, rating);
            await new SetCardRating(dbContext).RunAsync(request);
            return ControllerResultWithToast.Success($"{Get("RatingSavedOk")} {rating}\u2605", this);
        }
        #endregion
        #region CardVersionDiffWithCurrent
        [HttpGet("CardSelectedVersionDiffWithCurrent/{cardId}/{selectedVersionId}")]
        public async Task<IActionResult> CardSelectedVersionDiffWithCurrent(Guid cardId, Guid selectedVersionId)
        {
            var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
            var card = await new GetCardForEdit(dbContext).RunAsync(new GetCardForEdit.Request(userId, cardId));
            var selectedVersion = await new GetCardVersion(dbContext).RunAsync(new GetCardVersion.Request(userId, selectedVersionId));
            var result = new CardSelectedVersionDiffWithCurrentResult(card, selectedVersion, this);
            return Ok(result);
        }
        public sealed class CardSelectedVersionDiffWithCurrentResult
        {
            #region Private methods
            private void AddField(List<string> changedFields, List<string> unChangedFields, string fieldNameResourceId, string fieldValueInCard, string fieldValueInSelectedVersion, ILocalized localizer)
            {
                if (fieldValueInCard == fieldValueInSelectedVersion)
                    unChangedFields.Add($"<strong>{localizer.Get(fieldNameResourceId)}</strong> {(fieldValueInCard.Length > 0 ? fieldValueInCard : localizer.Get("Empty"))}");
                else
                {
                    var html = new StringBuilder();
                    html.Append($"<strong>{localizer.Get(fieldNameResourceId)}</strong>");
                    html.Append("<ul>");
                    html.Append($"<li><strong>{localizer.Get("SelectedVersion")}</strong> {(fieldValueInSelectedVersion.Length > 0 ? fieldValueInSelectedVersion : localizer.Get("Empty"))}</li>");
                    html.Append($"<li><strong>{localizer.Get("LastVersion")}</strong> {(fieldValueInCard.Length > 0 ? fieldValueInCard : localizer.Get("Empty"))}</li>");
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
                InfoAboutUsage = card.UsersOwningDeckIncluding.Any() ? localizer.Get("AppearsInDecksOf") + ' ' + string.Join(',', card.UsersOwningDeckIncluding) : localizer.Get("NotIncludedInAnyDeck");
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
                AddField(changedFields, unChangedFields, "LanguageName", card.LanguageName, selectedVersion.LanguageName, localizer);

                var cardTags = card.Tags.Any() ? string.Join(",", card.Tags.Select(t => t.TagName).OrderBy(name => name)) : localizer.Get("NoneMasc");
                var versionTags = selectedVersion.Tags.Any() ? string.Join(",", selectedVersion.Tags.OrderBy(name => name)) : localizer.Get("NoneMasc");
                AddField(changedFields, unChangedFields, card.Tags.Count() > 1 && selectedVersion.Tags.Count() > 1 ? "Tags" : "Tag", cardTags, versionTags, localizer);

                var cardVisibility = card.UsersWithVisibility.Any() ? string.Join(",", card.UsersWithVisibility.Select(u => u.UserName).OrderBy(name => name)) : localizer.Get("Public");
                var versionVisibility = selectedVersion.UsersWithVisibility.Any() ? string.Join(",", selectedVersion.UsersWithVisibility.OrderBy(name => name)) : localizer.Get("Public");
                AddField(changedFields, unChangedFields, "Visibility", cardVisibility, versionVisibility, localizer);

                var cardFrontSideImageNames = card.Images.Where(i => i.CardSide == ImageInCard.FrontSide).Select(i => i.Name).OrderBy(name => name);
                var cardFrontSideImageNamesJoined = cardFrontSideImageNames.Any() ? string.Join(",", cardFrontSideImageNames) : localizer.Get("NoneFeminine");
                var versionFrontSideImages = selectedVersion.FrontSideImageNames.Any() ? string.Join(",", selectedVersion.FrontSideImageNames.OrderBy(name => name)) : localizer.Get("NoneFeminine");
                AddField(changedFields, unChangedFields, "FrontSideImages", cardFrontSideImageNamesJoined, versionFrontSideImages, localizer);

                var cardBackSideImageNames = card.Images.Where(i => i.CardSide == ImageInCard.BackSide).Select(i => i.Name).OrderBy(name => name);
                var cardBackSideImageNamesJoined = cardBackSideImageNames.Any() ? string.Join(",", cardBackSideImageNames) : localizer.Get("NoneFeminine");
                var versionBackSideImages = selectedVersion.BackSideImageNames.Any() ? string.Join(",", selectedVersion.BackSideImageNames.OrderBy(name => name)) : localizer.Get("NoneFeminine");
                AddField(changedFields, unChangedFields, "BackSideImages", cardBackSideImageNamesJoined, versionBackSideImages, localizer);

                var cardAdditionalImageNames = card.Images.Where(i => i.CardSide == ImageInCard.AdditionalInfo).Select(i => i.Name).OrderBy(name => name);
                var cardAdditionalImageNamesJoined = cardAdditionalImageNames.Any() ? string.Join(",", cardAdditionalImageNames) : localizer.Get("NoneFeminine");
                var versionAdditionalImages = selectedVersion.AdditionalInfoImageNames.Any() ? string.Join(",", selectedVersion.AdditionalInfoImageNames.OrderBy(name => name)) : localizer.Get("NoneFeminine");
                AddField(changedFields, unChangedFields, "AdditionalInfoImages", cardAdditionalImageNamesJoined, versionAdditionalImages, localizer);

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
    }
}
