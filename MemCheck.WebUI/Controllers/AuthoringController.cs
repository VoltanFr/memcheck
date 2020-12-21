﻿using MemCheck.Application;
using MemCheck.Application.CardChanging;
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
            try
            {
                var result = new GetUsers(dbContext).Run();
                return Ok(result.Select(user => new GetUsersViewModel(user.UserId, user.UserName)));
            }
            catch (Exception e)
            {
                return ControllerResult.Failure(e, this);
            }
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
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                return Ok(new GetCurrentUserViewModel(user, dbContext));
            }
            catch (Exception e)
            {
                return ControllerResult.Failure(e, this);
            }
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
            try
            {
                CheckBodyParameter(card);
                var user = await userManager.GetUserAsync(HttpContext.User);
                var versionDescription = Localizer["InitialCardVersionCreation"].Value;
                var request = new CreateCard.Request(user.Id, card.FrontSide!, card.FrontSideImageList, card.BackSide!, card.BackSideImageList, card.AdditionalInfo!, card.AdditionalInfoImageList, card.LanguageId, card.Tags, card.UsersWithVisibility, versionDescription);
                var cardId = await new CreateCard(dbContext).RunAsync(request, Localizer);
                if (card.AddToDeck != Guid.Empty)
                    await new AddCardInDeck(dbContext).RunAsync(card.AddToDeck, cardId);
                return Ok();
            }
            catch (Exception e)
            {
                return ControllerResult.Failure(e, this);
            }
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
            try
            {
                CheckBodyParameter(card);
                var user = await userManager.GetUserAsync(HttpContext.User);
                var request = new UpdateCard.Request(cardId, user.Id, card.FrontSide, card.FrontSideImageList, card.BackSide, card.BackSideImageList, card.AdditionalInfo, card.AdditionalInfoImageList, card.LanguageId, card.Tags, card.UsersWithVisibility, card.VersionDescription);
                await new UpdateCard(dbContext).RunAsync(request, Localizer);
                return Ok();
            }
            catch (Exception e)
            {
                return ControllerResult.Failure(e, this);
            }
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
            try
            {
                var result = new GetAllAvailableTags(dbContext).Run();
                return Ok(result.Select(tag => new GetAllAvailableTagsViewModel(tag.TagId, tag.Name)));
            }
            catch (Exception e)
            {
                return ControllerResult.Failure(e, this);
            }
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
            try
            {
                var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
                var result = new GetCardForEdit(dbContext).RunAsync(new GetCardForEdit.Request(userId, cardId));
                return Ok(new GetCardForEditViewModel(await result, Localizer));
            }
            catch (Exception e)
            {
                return ControllerResult.Failure(e, this);
            }
        }
        #region Request and view model classes
        internal sealed class GetCardForEditViewModel
        {
            public GetCardForEditViewModel(GetCardForEdit.ResultModel applicationResult, IStringLocalizer localizer)
            {
                FrontSide = applicationResult.FrontSide;
                BackSide = applicationResult.BackSide;
                AdditionalInfo = applicationResult.AdditionalInfo;
                LanguageId = applicationResult.LanguageId;
                Tags = applicationResult.Tags.Select(tag => new GetAllAvailableTagsViewModel(tag.TagId, tag.TagName));
                UsersWithVisibility = applicationResult.UsersWithVisibility.Select(user => new GetUsersViewModel(user.UserId, user.UserName));
                CreationUtcDate = applicationResult.CreationUtcDate;
                LastChangeUtcDate = applicationResult.LastChangeUtcDate;
                InfoAboutUsage = applicationResult.UsersOwningDeckIncluding.Count() > 0 ? localizer["AppearsInDecksOf"].Value + ' ' + string.Join(',', applicationResult.UsersOwningDeckIncluding) : localizer["NotIncludedInAnyDeck"].Value;
                Images = applicationResult.Images.Select(applicationImage => new GetCardForEditImageViewModel(applicationImage, localizer));
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
            internal GetCardForEditImageViewModel(GetCardForEdit.ResultImageModel appResult, IStringLocalizer localizer)
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
            try
            {
                return Ok(new GetGuiMessagesViewModel(
                Localize("Success"),
                Localize("CardSavedOk"),
                Localize("Failure"),
                Localize("SureCreateWithoutTag"),
                Localize("Saved"),
                Localize("RatingSavedOk")
                ));
            }
            catch (Exception e)
            {
                return ControllerResult.Failure(e, this);
            }
        }
        public sealed class GetGuiMessagesViewModel
        {
            public GetGuiMessagesViewModel(string success, string cardSavedOk, string failure, string sureCreateWithoutTag, string saved, string ratingSavedOk)
            {
                Success = success;
                CardSavedOk = cardSavedOk;
                Failure = failure;
                SureCreateWithoutTag = sureCreateWithoutTag;
                Saved = saved;
                RatingSavedOk = ratingSavedOk;
            }
            public string Success { get; } = null!;
            public string CardSavedOk { get; } = null!;
            public string Failure { get; } = null!;
            public string SureCreateWithoutTag { get; } = null!;
            public string Saved { get; } = null!;
            public string RatingSavedOk { get; } = null!;
        }
        #endregion
        #region DecksOfUser, returns IEnumerable<GetUsersViewModel>
        [HttpGet("DecksOfUser")]
        public async Task<IActionResult> DecksOfUser()
        {
            try
            {
                var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
                var result = new GetUserDecks(dbContext).Run(userId);
                return Ok(result.Select(deck => new DecksOfUserViewModel(deck.DeckId, deck.Description)));
            }
            catch (Exception e)
            {
                return ControllerResult.Failure(e, this);
            }
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
            try
            {
                CheckBodyParameter(request);
                var appResult = await new GetImageInfo(dbContext, Localizer).RunAsync(request.ImageName);
                return Ok(new GetImageInfoViewModel(appResult.ImageId, appResult.Name, appResult.Source));
            }
            catch (Exception e)
            {
                return ControllerResult.Failure(e, this);
            }
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
            try
            {
                var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
                var appResults = await new GetCardVersions(dbContext, Localizer).RunAsync(cardId, userId);
                var result = appResults.Select(appResult => new CardVersion(appResult, Localizer));
                return Ok(result);
            }
            catch (Exception e)
            {
                return ControllerResult.Failure(e, this);
            }
        }
        public sealed class CardVersion
        {
            public CardVersion(GetCardVersions.ResultCardVersion appResult, IStringLocalizer localizer)
            {
                VersionUtcDate = appResult.VersionUtcDate;
                VersionCreator = appResult.VersionCreator;
                VersionDescription = appResult.VersionDescription;
                var fieldsDisplayNames = appResult.ChangedFieldNames.Select(fieldName => localizer[fieldName].Value);
                ChangedFieldList = string.Join(',', fieldsDisplayNames);
            }
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
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                var request = new SetCardRating.Request(user, cardId, rating);
                await new SetCardRating(dbContext).RunAsync(request);
                return Ok();
            }
            catch (Exception e)
            {
                return ControllerResult.Failure(e, this);
            }
        }
        #endregion
    }
}
