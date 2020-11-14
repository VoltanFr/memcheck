using Ganss.XSS;
using Markdig;
using Markdig.Extensions.AutoLinks;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using MemCheck.Application;
using MemCheck.Application.Heaping;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MemCheck.WebUI.Controllers
{
    [Route("[controller]")]
    public class LearnController : Controller, ILocalized
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        private readonly IStringLocalizer<DecksController> localizer;
        private readonly UserManager<MemCheckUser> userManager;
        private static readonly Guid noTagFakeGuid = Guid.Empty;
        #endregion
        public LearnController(MemCheckDbContext dbContext, IStringLocalizer<DecksController> localizer, UserManager<MemCheckUser> userManager) : base()
        {
            this.dbContext = dbContext;
            this.localizer = localizer;
            this.userManager = userManager;
        }
        public IStringLocalizer Localizer => localizer;
        #region GetImage
        [HttpGet("GetImage/{imageId}/{size}")]
        public IActionResult GetImage(Guid imageId, int size)
        {
            try
            {
                var blob = new GetImage(dbContext).Run(new GetImage.Request(imageId, size));
                var content = new MemoryStream(blob);
                return base.File(content, "APPLICATION/octet-stream", "noname");
            }
            catch (Exception e)
            {
                return ControllerError.BadRequest(e, this);
            }
        }
        #endregion
        #region MoveCardToHeap
        [HttpPatch("MoveCardToHeap/{deckId}/{cardId}/{targetHeap}/{manualMove}")]
        public async Task<IActionResult> MoveCardToHeap(Guid deckId, Guid cardId, int targetHeap, bool manualMove)
        {
            try
            {
                var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
                var request = new MoveCardToHeap.Request(userId, deckId, cardId, targetHeap, manualMove);
                await new MoveCardToHeap(dbContext).RunAsync(request);
                return Ok();
            }
            catch (Exception e)
            {
                return ControllerError.BadRequest(e, this);
            }
        }
        #endregion
        #region GetCards
        [HttpPost("GetCards")]
        public async Task<IActionResult> GetCardsAsync([FromBody] GetCardsRequest request)
        {
            try
            {
                if (request.ExcludedCardIds == null)
                    throw new ArgumentException("request.ExcludedCardIds is null");
                if (request.ExcludedTagIds == null)
                    throw new ArgumentException("request.ExcludedTagIds is null");
                var currentUserId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
                var cardsToDownload = request.CurrentCardCount == 0 ? 1 : (request.LearnModeIsUnknown ? 30 : 5);   //loading cards to repeat is much more time consuming
                var applicationRequest = new GetCardsToLearn.Request(currentUserId, request.DeckId, request.LearnModeIsUnknown, request.ExcludedCardIds, request.ExcludedTagIds, cardsToDownload);
                var applicationResult = await new GetCardsToLearn(dbContext).RunAsync(applicationRequest);
                var user = await userManager.GetUserAsync(HttpContext.User);
                var result = new GetCardsViewModel(applicationResult, localizer, user.UserName);
                return Ok(result);
            }
            catch (Exception e)
            {
                return ControllerError.BadRequest(e, this);
            }
        }
        #region Request and result classes
        public sealed class GetCardsRequest
        {
            public Guid DeckId { get; set; }
            public bool LearnModeIsUnknown { get; set; }
            public IEnumerable<Guid>? ExcludedCardIds { get; set; } = null!;
            public IEnumerable<Guid>? ExcludedTagIds { get; set; } = null!;
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
                OwnerName = appResult.OwnerName;
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
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                var userId = (user == null) ? Guid.Empty : user.Id;
                var decks = new GetUserDecksWithTags(dbContext).Run(userId);
                var result = decks.Select(deck => new UserDecksViewModel(deck.DeckId, deck.Description, DisplayServices.ShowDebugInfo(user), deck.Tags, localizer));
                return base.Ok(result);
            }
            catch (Exception e)
            {
                return ControllerError.BadRequest(e, this);
            }
        }
        public sealed class UserDecksViewModel
        {
            public UserDecksViewModel(Guid deckId, string description, bool showDebugInfo, IEnumerable<GetUserDecksWithTags.ResultTagModel> tags, IStringLocalizer localizer)
            {
                DeckId = deckId;
                Description = description;
                ShowDebugInfo = showDebugInfo;
                Tags = new[] { new UserDecksTagViewModel(noTagFakeGuid, localizer["None"]) }
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
        #region SetCardNotificationRegistration
        [HttpPatch("SetCardNotificationRegistration/{cardId}/{notif}")]
        public async Task<IActionResult> SetCardNotificationRegistration(Guid cardId, bool notif)
        {
            try
            {
                var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
                if (notif)
                {
                    var request = new AddCardNotifications.Request(userId, new[] { cardId });
                    await new AddCardNotifications(dbContext).RunAsync(request);
                }
                else
                {
                    var request = new RemoveCardNotification.Request(userId, cardId);
                    await new RemoveCardNotification(dbContext).RunAsync(request);
                }
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
