using MemCheck.Application;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.WebUI.Controllers
{
    [Route("[controller]")]
    public class AuthoringController : Controller, ILocalized
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        private readonly UserManager<MemCheckUser> userManager;
        private readonly IStringLocalizer<AuthoringController> localizer;
        #endregion
        public AuthoringController(MemCheckDbContext dbContext, UserManager<MemCheckUser> userManager, IStringLocalizer<AuthoringController> localizer) : base()
        {
            this.dbContext = dbContext;
            this.userManager = userManager;
            this.localizer = localizer;
        }
        public IStringLocalizer Localizer
        {
            get
            {
                return localizer;
            }
        }
        #region GetUsers, returns IEnumerable<GetUsersViewModel>
        [HttpGet("GetUsers")]
        public IActionResult GetUsers()
        {
            var result = new GetUsers(dbContext).Run();
            return base.Ok(result.Select(user => new GetUsersViewModel(user.UserId, user.UserName)));
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
            return base.Ok(new GetCurrentUserViewModel(user, dbContext));
        }
        public sealed class GetCurrentUserViewModel
        {
            public GetCurrentUserViewModel(MemCheckUser user, MemCheckDbContext dbContext)
            {
                UserId = user.Id;
                UserName = user.UserName;
                var cardLanguage = user.PreferredCardCreationLanguage ?? dbContext.CardLanguages.First();
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
            if (card.FrontSide == null)
                return ControllerError.BadRequest(localizer["InvalidCardEmptyFrontSide"], this);
            if (card.BackSide == null)
                card.BackSide = "";
            if (card.AdditionalInfo == null)
                card.AdditionalInfo = "";
            if (card.FrontSideImageList == null)
                return ControllerError.BadRequest("Invalid input: card.FrontSideImageList == null", this);
            if (card.BackSideImageList == null)
                return ControllerError.BadRequest("Invalid input: card.BackSideImageList == null", this);
            if (card.AdditionalInfoImageList == null)
                return ControllerError.BadRequest("Invalid input: card.AdditionalInfoImageList == null", this);
            if (card.UsersWithVisibility == null)
                return ControllerError.BadRequest("Invalid input: card.UsersWithVisibility == null", this);
            if (card.Tags == null)
                return ControllerError.BadRequest("Invalid input: card.Tags == null", this);

            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                var versionDescription = localizer["InitialCardVersionCreation"].Value;
                var request = new CreateCard.Request(user, card.FrontSide, card.FrontSideImageList, card.BackSide, card.BackSideImageList, card.AdditionalInfo, card.AdditionalInfoImageList, card.LanguageId, card.Tags, card.UsersWithVisibility, versionDescription);
                var cardId = await new CreateCard(dbContext).RunAsync(request, localizer);

                if (card.AddToDeck != Guid.Empty)
                    await new AddCardInDeck(dbContext).RunAsync(card.AddToDeck, cardId);

                return Ok();
            }
            catch (RequestInputException e)
            {
                return ControllerError.BadRequest(e.Message, this);
            }
            catch (Exception e)
            {
                return ControllerError.BadRequest(e, this);
            }
        }
        public sealed class PostCardOfUserRequest
        {
            public string? FrontSide { get; set; }
            public IEnumerable<Guid>? FrontSideImageList { get; set; }
            public string? BackSide { get; set; }
            public IEnumerable<Guid>? BackSideImageList { get; set; }
            public string? AdditionalInfo { get; set; }
            public IEnumerable<Guid>? AdditionalInfoImageList { get; set; }
            public Guid LanguageId { get; set; }
            public Guid AddToDeck { get; set; }
            public IEnumerable<Guid>? Tags { get; set; }
            public IEnumerable<Guid>? UsersWithVisibility { get; set; }
        }
        #endregion
        #region UpdateCard
        [HttpPut("UpdateCard/{cardId}")]
        public async Task<IActionResult> UpdateCard(Guid cardId, [FromBody] UpdateCardRequest card)
        {
            if (card.FrontSide == null)
                return ControllerError.BadRequest(localizer["InvalidCardEmptyFrontSide"], this);
            if (card.BackSide == null)
                card.BackSide = "";
            if (card.AdditionalInfo == null)
                card.AdditionalInfo = "";
            if (card.FrontSideImageList == null)
                return ControllerError.BadRequest("Invalid input: card.FrontSideImageList == null", this);
            if (card.BackSideImageList == null)
                return ControllerError.BadRequest("Invalid input: card.BackSideImageList == null", this);
            if (card.AdditionalInfoImageList == null)
                return ControllerError.BadRequest("Invalid input: card.AdditionalInfoImageList == null", this);
            if (card.UsersWithVisibility == null)
                return ControllerError.BadRequest("Invalid input: card.UsersWithVisibility == null", this);
            if (card.Tags == null)
                return ControllerError.BadRequest("Invalid input: card.Tags == null", this);
            if (string.IsNullOrWhiteSpace(card.VersionDescription))
                return ControllerError.BadRequest(localizer["InvalidCardEmptyVersionDescription"], this);

            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                var request = new UpdateCard.Request(cardId, user, card.FrontSide, card.FrontSideImageList, card.BackSide, card.BackSideImageList, card.AdditionalInfo, card.AdditionalInfoImageList, card.LanguageId, card.Tags, card.UsersWithVisibility, card.VersionDescription);
                await new UpdateCard(dbContext).RunAsync(request, localizer);
                return Ok();
            }
            catch (Exception e)
            {
                return ControllerError.BadRequest(e, this);
            }
        }
        public sealed class UpdateCardRequest
        {
            public string? FrontSide { get; set; }
            public IEnumerable<Guid>? FrontSideImageList { get; set; }
            public string? BackSide { get; set; }
            public IEnumerable<Guid>? BackSideImageList { get; set; }
            public string? AdditionalInfo { get; set; }
            public IEnumerable<Guid>? AdditionalInfoImageList { get; set; }
            public Guid LanguageId { get; set; }
            public Guid AddToDeck { get; set; }
            public IEnumerable<Guid>? Tags { get; set; }
            public IEnumerable<Guid>? UsersWithVisibility { get; set; }
            public string? VersionDescription { get; set; }
        }
        #endregion
        #region GetAllAvailableTags
        [HttpGet("AllAvailableTags")]
        public IActionResult GetAllAvailableTags()
        {
            var result = new GetAllAvailableTags(dbContext).Run();
            return base.Ok(result.Select(tag => new GetAllAvailableTagsViewModel(tag.TagId, tag.Name)));
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
                return Ok(new GetCardForEditViewModel(await result, localizer));
            }
            catch (Exception e)
            {
                return ControllerError.BadRequest(e, this);
            }
        }
        #region Request and view model classes
        internal sealed class GetCardForEditViewModel
        {
            public GetCardForEditViewModel(GetCardForEdit.ResultModel applicationResult, IStringLocalizer<AuthoringController> localizer)
            {
                FrontSide = applicationResult.FrontSide;
                BackSide = applicationResult.BackSide;
                AdditionalInfo = applicationResult.AdditionalInfo;
                LanguageId = applicationResult.LanguageId;
                Tags = applicationResult.Tags.Select(tag => new GetAllAvailableTagsViewModel(tag.TagId, tag.TagName));
                UsersWithVisibility = applicationResult.UsersWithVisibility.Select(user => new GetUsersViewModel(user.UserId, user.UserName));
                CreationUtcDate = applicationResult.CreationUtcDate;
                LastChangeUtcDate = applicationResult.LastChangeUtcDate;
                InfoAboutUsage = applicationResult.UsersOwningDeckIncluding.Count() > 0 ? localizer["AppearsInDecksOf"] + ' ' + string.Join(',', applicationResult.UsersOwningDeckIncluding) : localizer["NotIncludedInAnyDeck"];
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
                OwnerName = appResult.Owner.UserName;
                Name = appResult.Name;
                Description = appResult.Description;
                Source = appResult.Source;
                CardSide = appResult.CardSide;
            }
            public Guid ImageId { get; }
            public string OwnerName { get; }
            public string Name { get; }
            public string Description { get; }
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
                localizer["Success"],
                localizer["CardSavedOk"],
                localizer["Failure"],
                localizer["SureCreateWithoutTag"],
                localizer["Saved"],
                localizer["RatingSavedOk"]
                ));
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
            var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
            var result = new GetUserDecks(dbContext).Run(userId);
            return base.Ok(result.Select(deck => new DecksOfUserViewModel(deck.DeckId, deck.Description)));
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
            if (request.ImageName == null)
                return BadRequest(new { ToastText = localizer["PleaseEnterAnImageName"].Value, ShowStatus = false, ToastTitle = localizer["Failure"].Value });

            try
            {
                var appResult = await new GetImageInfo(dbContext, localizer).RunAsync(request.ImageName);
                var popoverInfo = localizer["ImageUploader"] + ' ' + appResult.Owner.UserName + Environment.NewLine +
                    localizer["ImageName"] + ' ' + appResult.Name + Environment.NewLine +
                    localizer["Description"] + ' ' + appResult.Description + Environment.NewLine +
                    localizer["Source"] + ' ' + appResult.Source + Environment.NewLine +
                    localizer["UsedIn"] + ' ' + appResult.CardCount + localizer["Cards"];

                return Ok(new GetImageInfoViewModel(appResult.ImageId, popoverInfo));
            }
            catch (Exception e)
            {
                return ControllerError.BadRequest(e, this);
            }
        }
        #region Request and view model classes
        public sealed class GetImageInfoRequest
        {
            public string? ImageName { get; set; }
        }
        public sealed class GetImageInfoViewModel
        {
            public GetImageInfoViewModel(Guid imageId, string popoverInfo)
            {
                ImageId = imageId;
                PopoverInfo = popoverInfo;
            }
            public Guid ImageId { get; }
            public string PopoverInfo { get; }
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
                var appResults = await new GetCardVersions(dbContext, localizer).RunAsync(cardId, userId);
                var result = appResults.Select(appResult => new CardVersion(appResult, localizer));
                return Ok(result);
            }
            catch (Exception e)
            {
                return ControllerError.BadRequest(e, this);
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
                return ControllerError.BadRequest(e, this);
            }
        }
        #endregion
    }
}
