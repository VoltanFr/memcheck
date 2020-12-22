using MemCheck.Application;
using MemCheck.Application.Notifying;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.WebUI.Controllers
{
    [Route("[controller]"), Authorize]
    public class LearnController : MemCheckController
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        private readonly UserManager<MemCheckUser> userManager;
        private static readonly Guid noTagFakeGuid = Guid.Empty;
        #endregion
        public LearnController(MemCheckDbContext dbContext, IStringLocalizer<DecksController> localizer, UserManager<MemCheckUser> userManager) : base(localizer)
        {
            this.dbContext = dbContext;
            this.userManager = userManager;
        }
        #region GetImage
        [HttpGet("GetImage/{imageId}/{size}")]
        public IActionResult GetImage(Guid imageId, int size)
        {
            var blob = new GetImage(dbContext).Run(new GetImage.Request(imageId, size));
            var content = new MemoryStream(blob);
            return base.File(content, "APPLICATION/octet-stream", "noname");
        }
        #endregion
        #region MoveCardToHeap
        [HttpPatch("MoveCardToHeap/{deckId}/{cardId}/{targetHeap}/{manualMove}")]
        public async Task<IActionResult> MoveCardToHeap(Guid deckId, Guid cardId, int targetHeap, bool manualMove)
        {
            var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
            var request = new MoveCardToHeap.Request(userId, deckId, cardId, targetHeap, manualMove);
            await new MoveCardToHeap(dbContext).RunAsync(request);
            return Ok();
        }
        #endregion
        #region GetCards
        [HttpPost("GetCards")]
        public async Task<IActionResult> GetCardsAsync([FromBody] GetCardsRequest request)
        {
            CheckBodyParameter(request);
            var currentUserId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
            var cardsToDownload = request.CurrentCardCount == 0 ? 1 : (request.LearnModeIsUnknown ? 30 : 5);   //loading cards to repeat is much more time consuming
            var applicationRequest = new GetCardsToLearn.Request(currentUserId, request.DeckId, request.LearnModeIsUnknown, request.ExcludedCardIds, request.ExcludedTagIds, cardsToDownload);
            var applicationResult = await new GetCardsToLearn(dbContext).RunAsync(applicationRequest);
            var user = await userManager.GetUserAsync(HttpContext.User);
            var result = new GetCardsViewModel(applicationResult, Localizer, user.UserName);
            return Ok(result);
        }
        #region Request and result classes
        public sealed class GetCardsRequest
        {
            public Guid DeckId { get; set; }
            public bool LearnModeIsUnknown { get; set; }
            public IEnumerable<Guid> ExcludedCardIds { get; set; } = null!;
            public IEnumerable<Guid> ExcludedTagIds { get; set; } = null!;
            public int CurrentCardCount { get; set; }
        }
        public sealed class GetCardsViewModel
        {
            public GetCardsViewModel(IEnumerable<GetCardsToLearn.ResultCard> applicationResultCards, IStringLocalizer localizer, string currentUser)
            {
                Cards = applicationResultCards.Select(card => new GetCardsCardViewModel(card, localizer, currentUser));
            }
            public IEnumerable<GetCardsCardViewModel> Cards { get; }
        }
        public sealed class GetCardsCardViewModel
        {
            #region Private methods
            private string RenderMarkdown(string markdown)
            {
                return markdown;
                //if (string.IsNullOrEmpty(markdown))
                //    return string.Empty;

                //using (var htmlWriter = new StringWriter())
                //{

                //    var pipeline = new MarkdownPipelineBuilder()
                //        .UseSoftlineBreakAsHardlineBreak()
                //        .UseAutoLinks(new AutoLinkOptions() { OpenInNewWindow = true });

                //    var document = Markdown.Parse(markdown, pipeline.Build());

                //    foreach (var descendant in document.Descendants())
                //        if (descendant is AutolinkInline || descendant is LinkInline)
                //            descendant.GetAttributes().AddPropertyIfNotExist("target", "_blank");

                //    var renderer = new HtmlRenderer(htmlWriter);
                //    renderer.Render(document);

                //    var rendered = htmlWriter.ToString();
                //    var sanitized = new HtmlSanitizer().Sanitize(rendered);
                //    return sanitized;
                //}
            }
            #endregion
            public GetCardsCardViewModel(GetCardsToLearn.ResultCard applicationResult, IStringLocalizer localizer, string currentUser)
            {
                CardId = applicationResult.CardId;
                HeapId = applicationResult.Heap;
                Heap = DisplayServices.HeapName(applicationResult.Heap, localizer);
                LastLearnUtcTime = applicationResult.LastLearnUtcTime;
                LastChangeUtcTime = applicationResult.LastChangeUtcTime;
                BiggestHeapReached = applicationResult.BiggestHeapReached;
                NbTimesInNotLearnedHeap = applicationResult.NbTimesInNotLearnedHeap;
                FrontSide = RenderMarkdown(applicationResult.FrontSide);
                BackSide = RenderMarkdown(applicationResult.BackSide);
                AdditionalInfo = RenderMarkdown(applicationResult.AdditionalInfo);
                Owner = applicationResult.Owner;
                Tags = applicationResult.Tags.OrderBy(tag => tag);
                RemoveAlertMessage = localizer["RemoveAlertMessage"] + " " + Heap + "\n" + localizer["DateAddedToDeck"] + " ";
                VisibleToCount = applicationResult.VisibleTo.Count();
                AddToDeckUtcTime = applicationResult.AddToDeckUtcTime;
                CurrentUserRating = applicationResult.UserRating;
                AverageRating = Math.Round(applicationResult.AverageRating, 1);
                CountOfUserRatings = applicationResult.CountOfUserRatings;
                if (VisibleToCount == 1)
                {
                    var visibleToUser = applicationResult.VisibleTo.First();
                    if (visibleToUser != currentUser)
                        throw new ApplicationException($"Card visible to single user should be current user, is {visibleToUser}");
                    VisibleTo = localizer["YouOnly"].ToString();
                }
                else
                {
                    if (VisibleToCount == 0)
                        VisibleTo = localizer["AllUsers"].ToString();
                    else
                        VisibleTo = string.Join(',', applicationResult.VisibleTo);
                }
                Images = applicationResult.Images.Select(applicationImage => new GetCardsImageViewModel(applicationImage, localizer));
                MoveToHeapTargets = applicationResult.MoveToHeapExpiryInfos.Select(moveToHeapInfo =>
                        new GetCardsHeapModel(moveToHeapInfo.HeapId, DisplayServices.HeapName(moveToHeapInfo.HeapId, localizer), moveToHeapInfo.UtcExpiryDate, localizer)
                    ).OrderBy(heapModel => heapModel.HeapId);
                RegisteredForNotifications = applicationResult.RegisteredForNotifications;
            }
            public Guid CardId { get; }
            public int HeapId { get; }
            public string Heap { get; }
            public DateTime LastLearnUtcTime { get; }
            public DateTime LastChangeUtcTime { get; }
            public int BiggestHeapReached { get; }
            public int NbTimesInNotLearnedHeap { get; }
            public string FrontSide { get; }
            public string BackSide { get; }
            public string AdditionalInfo { get; }
            public string Owner { get; }
            public IEnumerable<string> Tags { get; }
            public IEnumerable<GetCardsHeapModel> MoveToHeapTargets { get; }
            public int VisibleToCount { get; }
            public string VisibleTo { get; }
            public string RemoveAlertMessage { get; }
            public DateTime AddToDeckUtcTime { get; }
            public IEnumerable<GetCardsImageViewModel> Images { get; }
            public int CurrentUserRating { get; }
            public double AverageRating { get; }
            public int CountOfUserRatings { get; }
            public bool RegisteredForNotifications { get; }
        }
        public sealed class GetCardsImageViewModel
        {
            public GetCardsImageViewModel(GetCardsToLearn.ResultImageModel appResult, IStringLocalizer localizer)
            {
                ImageId = appResult.ImageId;
                Name = appResult.Name;
                CardSide = appResult.CardSide;
            }
            public Guid ImageId { get; }
            public string Name { get; }
            public int CardSide { get; set; }   //1 = front side ; 2 = back side ; 3 = AdditionalInfo
        }
        public sealed class GetCardsHeapModel
        {
            public GetCardsHeapModel(int heapId, string heapName, DateTime expiryUtcDate, IStringLocalizer localizer)
            {
                HeapId = heapId;
                HeapName = heapName;
                if (heapId == 0)
                {
                    ExpiryUtcDate = DateTime.MinValue.ToUniversalTime();
                    MoveToAlertMessage = localizer["MoveThisCardToHeap"] + ' ' + heapName + " ?";
                }
                else
                {
                    if (expiryUtcDate < DateTime.UtcNow)
                    {
                        ExpiryUtcDate = DateTime.MinValue.ToUniversalTime();
                        MoveToAlertMessage = localizer["MoveThisCardToHeap"] + ' ' + heapName + " ? " + localizer["ItWillBeExpired"];
                    }
                    else
                    {
                        ExpiryUtcDate = expiryUtcDate;
                        MoveToAlertMessage = localizer["MoveThisCardToHeap"] + ' ' + heapName + " ? " + localizer["ItWillExpireOn"];
                    }
                }
            }
            public int HeapId { get; }
            public string HeapName { get; }
            public string MoveToAlertMessage { get; }
            public DateTime ExpiryUtcDate { get; }
        }
        #endregion
        #endregion
        #region UserDecks
        [HttpGet("UserDecks")]
        public async Task<IActionResult> UserDecks()
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var userId = (user == null) ? Guid.Empty : user.Id;
            var decks = new GetUserDecksWithTags(dbContext).Run(userId);
            var result = decks.Select(deck => new UserDecksViewModel(deck.DeckId, deck.Description, DisplayServices.ShowDebugInfo(user), deck.Tags, Localizer));
            return base.Ok(result);
        }
        public sealed class UserDecksViewModel
        {
            public UserDecksViewModel(Guid deckId, string description, bool showDebugInfo, IEnumerable<GetUserDecksWithTags.ResultTagModel> tags, IStringLocalizer localizer)
            {
                DeckId = deckId;
                Description = description;
                ShowDebugInfo = showDebugInfo;
                Tags = new[] { new UserDecksTagViewModel(noTagFakeGuid, localizer["None"].Value) }
                    .Concat(tags.Select(tag => new UserDecksTagViewModel(tag.TagId, tag.TagName)));
            }
            public Guid DeckId { get; }
            public string Description { get; }
            public IEnumerable<UserDecksTagViewModel> Tags { get; }
            public bool ShowDebugInfo { get; }
        }
        public sealed class UserDecksTagViewModel
        {
            public UserDecksTagViewModel(Guid tagId, string tagName)
            {
                this.tagId = tagId;
                this.tagName = tagName;
            }
            public Guid tagId { get; }
            public string tagName { get; }
        }
        #endregion
        #region SetCardRating
        [HttpPatch("SetCardRating/{cardId}/{rating}")]
        public async Task<IActionResult> SetCardRating(Guid cardId, int rating)
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var request = new SetCardRating.Request(user, cardId, rating);
            await new SetCardRating(dbContext).RunAsync(request);
            return Ok();
        }
        #endregion
        #region SetCardNotificationRegistration
        [HttpPatch("SetCardNotificationRegistration/{cardId}/{notif}")]
        public async Task<IActionResult> SetCardNotificationRegistration(Guid cardId, bool notif)
        {
            var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
            if (notif)
            {
                var request = new AddCardSubscriptions.Request(userId, new[] { cardId });
                await new AddCardSubscriptions(dbContext).RunAsync(request);
            }
            else
            {
                var request = new RemoveCardSubscriptions.Request(userId, new[] { cardId });
                await new RemoveCardSubscriptions(dbContext).RunAsync(request);
            }
            return Ok();
        }
        #endregion
    }
}
