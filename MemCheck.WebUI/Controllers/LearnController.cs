using MemCheck.Application;
using MemCheck.Application.Cards;
using MemCheck.Application.Decks;
using MemCheck.Application.Heaping;
using MemCheck.Application.Images;
using MemCheck.Application.Notifiying;
using MemCheck.Application.QueryValidation;
using MemCheck.Application.Ratings;
using MemCheck.Basics;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.WebUI.Controllers;

[Route("[controller]")]
public class LearnController : MemCheckController
{
    #region Fields
    private readonly CallContext callContext;
    private readonly UserManager<MemCheckUser> userManager;
    private static readonly Guid noTagFakeGuid = Guid.Empty;
    private const int learnModeExpired = 1;
    private const int learnModeUnknown = 2;
    private const int learnModeDemo = 3;
    #endregion
    public LearnController(MemCheckDbContext dbContext, IStringLocalizer<LearnController> localizer, UserManager<MemCheckUser> userManager, TelemetryClient telemetryClient) : base(localizer)
    {
        callContext = new CallContext(dbContext, new MemCheckTelemetryClient(telemetryClient), this, new ProdRoleChecker(userManager));
        this.userManager = userManager;
    }
    #region GetImage
    [HttpGet("GetImage/{imageId}/{size}")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "We return a disposable object, which will be disposed by ASP NET Core")]
    public async Task<IActionResult> GetImageAsync(Guid imageId, int size)
    {
        static GetImage.Request.ImageSize AppSizeFromWebParam(int size)
        {
            return size switch
            {
                1 => GetImage.Request.ImageSize.Small,
                2 => GetImage.Request.ImageSize.Medium,
                3 => GetImage.Request.ImageSize.Big,
                _ => throw new NotImplementedException(size.ToString(CultureInfo.InvariantCulture))
            };
        }

        var blob = await new GetImage(callContext).RunAsync(new GetImage.Request(imageId, AppSizeFromWebParam(size)));
        var content = new MemoryStream(blob.ImageBytes.ToArray());
        return base.File(content, "image/jpeg", "noname.jpg");
    }
    #endregion
    #region GetImageByName
    [HttpPost("GetImageByName")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "We return a disposable object, which will be disposed by ASP NET Core")]
    public async Task<IActionResult> GetImageByNameAsync([FromBody] GetImageByNameRequest request)
    {
        static GetImageFromName.Request.ImageSize AppSizeFromWebParam(int size)
        {
            return size switch
            {
                1 => GetImageFromName.Request.ImageSize.Small,
                2 => GetImageFromName.Request.ImageSize.Medium,
                3 => GetImageFromName.Request.ImageSize.Big,
                _ => throw new NotImplementedException(size.ToString(CultureInfo.InvariantCulture))
            };
        }

        CheckBodyParameter(request);
        var blob = await new GetImageFromName(callContext).RunAsync(new GetImageFromName.Request(request.ImageName, AppSizeFromWebParam(request.Size)));
        var content = new MemoryStream(blob.ImageBytes.ToArray());
        return base.File(content, "image/jpeg", "noname.jpg");
    }
    public sealed class GetImageByNameRequest
    {
        public string ImageName { get; set; } = null!;
        public int Size { get; set; }
    }
    #endregion
    #region MoveCardToHeap
    [HttpPatch("MoveCardToHeap/{deckId}/{cardId}/{targetHeap}/{manualMove}")]
    public async Task<IActionResult> MoveCardToHeap(Guid deckId, Guid cardId, int targetHeap, bool manualMove)
    {
        var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
        if (manualMove)
        {
            var request = new MoveCardsToHeap.Request(userId, deckId, targetHeap, cardId.AsArray());
            await new MoveCardsToHeap(callContext).RunAsync(request);
        }
        else
        {
            var request = new MoveCardToHeap.Request(userId, deckId, cardId, targetHeap);
            await new MoveCardToHeap(callContext).RunAsync(request);
        }
        return Ok();
    }
    #endregion
    #region GetCards
    [HttpPost("GetCards")]
    public async Task<IActionResult> GetCardsAsync([FromBody] GetCardsRequest request)
    {
        CheckBodyParameter(request);
        var user = await userManager.GetUserAsync(HttpContext.User);

        switch (request.LearnMode)
        {
            case learnModeUnknown:
                {
                    var applicationRequest = new GetUnknownCardsToLearn.Request(user.Id, request.DeckId, request.ExcludedCardIds, 30);
                    var applicationResult = (await new GetUnknownCardsToLearn(callContext).RunAsync(applicationRequest)).Cards;
                    var result = new GetCardsViewModel(applicationResult, this, user.UserName);
                    return Ok(result);
                }
            case learnModeExpired:
                {
                    var cardsToDownload = request.CurrentCardCount == 0 ? 1 : 5;   //loading cards to repeat is much more time consuming
                    var applicationRequest = new GetCardsToRepeat.Request(user.Id, request.DeckId, request.ExcludedCardIds, cardsToDownload);
                    var applicationResult = (await new GetCardsToRepeat(callContext).RunAsync(applicationRequest)).Cards;
                    var result = new GetCardsViewModel(applicationResult, this, user.UserName);
                    return Ok(result);
                }
            case learnModeDemo:
                {
                    if (user != null)
                        return BadRequest();
                    var applicationRequest = new GetCardsForDemo.Request(request.DeckId, request.ExcludedCardIds, 30);
                    var applicationResult = (await new GetCardsForDemo(callContext).RunAsync(applicationRequest)).Cards;
                    var result = new GetCardsViewModel(applicationResult, this);
                    return Ok(result);
                }
            default:
                return BadRequest();
        }
    }
    #region Request and result classes
    public sealed class GetCardsRequest
    {
        public Guid DeckId { get; set; }    //When learnMode == learnModeDemo, this is not a deck but a tag
        public int LearnMode { get; set; } //can be learnModeExpired, learnModeUnknown, learnModeDemo
        public IEnumerable<Guid> ExcludedCardIds { get; set; } = null!;
        public int CurrentCardCount { get; set; }
    }
    public sealed class GetCardsViewModel
    {
        public GetCardsViewModel(IEnumerable<GetUnknownCardsToLearn.ResultCard> applicationResultCards, ILocalized localizer, string currentUser)
        {
            Cards = applicationResultCards.Select(card => new GetCardsCardViewModel(card, localizer, currentUser));
        }
        public GetCardsViewModel(IEnumerable<GetCardsToRepeat.ResultCard> applicationResultCards, ILocalized localizer, string currentUser)
        {
            Cards = applicationResultCards.Select(card => new GetCardsCardViewModel(card, localizer, currentUser));
        }
        public GetCardsViewModel(IEnumerable<GetCardsForDemo.ResultCard> applicationResultCards, ILocalized localizer)
        {
            Cards = applicationResultCards.Select(card => new GetCardsCardViewModel(card, localizer));
        }
        public IEnumerable<GetCardsCardViewModel> Cards { get; }
    }
    public sealed class GetCardsCardViewModel
    {
        public GetCardsCardViewModel(GetUnknownCardsToLearn.ResultCard applicationResult, ILocalized localizer, string currentUser)
        {
            CardId = applicationResult.CardId;
            HeapId = 0;
            Heap = DisplayServices.HeapName(0, localizer);
            LastLearnUtcTime = applicationResult.LastLearnUtcTime;
            LastChangeUtcTime = applicationResult.LastChangeUtcTime;
            BiggestHeapReached = applicationResult.BiggestHeapReached;
            NbTimesInNotLearnedHeap = applicationResult.NbTimesInNotLearnedHeap;
            FrontSide = applicationResult.FrontSide;
            BackSide = applicationResult.BackSide;
            AdditionalInfo = applicationResult.AdditionalInfo;
            References = applicationResult.References;
            Owner = applicationResult.Owner;
            Tags = applicationResult.Tags.OrderBy(tag => tag);
            RemoveAlertMessage = localizer.GetLocalized("RemoveAlertMessage") + " " + Heap + "\n" + localizer.GetLocalized("DateAddedToDeck") + " ";
            VisibleToCount = applicationResult.VisibleTo.Count();
            AddToDeckUtcTime = applicationResult.AddToDeckUtcTime;
            CurrentUserRating = applicationResult.UserRating;
            AverageRating = Math.Round(applicationResult.AverageRating, 1);
            CountOfUserRatings = applicationResult.CountOfUserRatings;
            IsInFrench = applicationResult.IsInFrench;
            if (VisibleToCount == 1)
            {
                var visibleToUser = applicationResult.VisibleTo.First();
                if (visibleToUser != currentUser)
                    throw new InvalidProgramException($"Card visible to single user should be current user, is {visibleToUser}");
                VisibleTo = localizer.GetLocalized("YouOnly");
            }
            else
                VisibleTo = VisibleToCount == 0 ? localizer.GetLocalized("AllUsers") : string.Join(',', applicationResult.VisibleTo);
            Images = applicationResult.Images.Select(applicationImage => new GetCardsImageViewModel(applicationImage));
            MoveToHeapTargets = applicationResult.MoveToHeapExpiryInfos.Select(moveToHeapInfo =>
                    new GetCardsHeapModel(moveToHeapInfo.HeapId, DisplayServices.HeapName(moveToHeapInfo.HeapId, localizer), moveToHeapInfo.UtcExpiryDate, localizer)
                ).OrderBy(heapModel => heapModel.HeapId);
            RegisteredForNotifications = applicationResult.RegisteredForNotifications;
        }
        public GetCardsCardViewModel(GetCardsToRepeat.ResultCard applicationResult, ILocalized localizer, string currentUser)
        {
            CardId = applicationResult.CardId;
            HeapId = applicationResult.Heap;
            Heap = DisplayServices.HeapName(applicationResult.Heap, localizer);
            LastLearnUtcTime = applicationResult.LastLearnUtcTime;
            LastChangeUtcTime = applicationResult.LastChangeUtcTime;
            BiggestHeapReached = applicationResult.BiggestHeapReached;
            NbTimesInNotLearnedHeap = applicationResult.NbTimesInNotLearnedHeap;
            FrontSide = applicationResult.FrontSide;
            BackSide = applicationResult.BackSide;
            AdditionalInfo = applicationResult.AdditionalInfo;
            References = applicationResult.References;
            Owner = applicationResult.Owner;
            Tags = applicationResult.Tags.OrderBy(tag => tag);
            RemoveAlertMessage = localizer.GetLocalized("RemoveAlertMessage") + " " + Heap + "\n" + localizer.GetLocalized("DateAddedToDeck") + " ";
            VisibleToCount = applicationResult.VisibleTo.Count();
            AddToDeckUtcTime = applicationResult.AddToDeckUtcTime;
            CurrentUserRating = applicationResult.UserRating;
            AverageRating = Math.Round(applicationResult.AverageRating, 1);
            CountOfUserRatings = applicationResult.CountOfUserRatings;
            IsInFrench = applicationResult.IsInFrench;
            if (VisibleToCount == 1)
            {
                var visibleToUser = applicationResult.VisibleTo.First();
                if (visibleToUser != currentUser)
                    throw new InvalidProgramException($"Card visible to single user should be current user, is {visibleToUser}");
                VisibleTo = localizer.GetLocalized("YouOnly");
            }
            else
                VisibleTo = VisibleToCount == 0 ? localizer.GetLocalized("AllUsers") : string.Join(',', applicationResult.VisibleTo);
            Images = applicationResult.Images.Select(applicationImage => new GetCardsImageViewModel(applicationImage));
            MoveToHeapTargets = applicationResult.MoveToHeapExpiryInfos.Select(moveToHeapInfo =>
                    new GetCardsHeapModel(moveToHeapInfo.HeapId, DisplayServices.HeapName(moveToHeapInfo.HeapId, localizer), moveToHeapInfo.UtcExpiryDate, localizer)
                ).OrderBy(heapModel => heapModel.HeapId);
            RegisteredForNotifications = applicationResult.RegisteredForNotifications;
        }
        public GetCardsCardViewModel(GetCardsForDemo.ResultCard applicationResult, ILocalized localizer)
        {
            CardId = applicationResult.CardId;
            HeapId = 0;
            Heap = DisplayServices.HeapName(0, localizer);
            LastLearnUtcTime = CardInDeck.NeverLearntLastLearnTime;
            LastChangeUtcTime = applicationResult.LastChangeUtcTime;
            BiggestHeapReached = CardInDeck.UnknownHeap;
            NbTimesInNotLearnedHeap = 1;
            FrontSide = applicationResult.FrontSide;
            BackSide = applicationResult.BackSide;
            AdditionalInfo = applicationResult.AdditionalInfo;
            References = applicationResult.References;
            Owner = applicationResult.VersionCreator;
            Tags = applicationResult.Tags.OrderBy(tag => tag);
            RemoveAlertMessage = "ERROR";
            VisibleToCount = 0;
            AddToDeckUtcTime = CardInDeck.NeverLearntLastLearnTime;
            CurrentUserRating = 0;
            AverageRating = Math.Round(applicationResult.AverageRating, 1);
            CountOfUserRatings = applicationResult.CountOfUserRatings;
            IsInFrench = applicationResult.IsInFrench;
            VisibleTo = localizer.GetLocalized("AllUsers");
            Images = applicationResult.Images.Select(applicationImage => new GetCardsImageViewModel(applicationImage));
            MoveToHeapTargets = Array.Empty<GetCardsHeapModel>();
            RegisteredForNotifications = false;
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
        public string References { get; }
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
        public bool IsInFrench { get; }
    }
    public sealed class GetCardsImageViewModel
    {
        public GetCardsImageViewModel(GetUnknownCardsToLearn.ResultImageModel appResult)
        {
            ImageId = appResult.ImageDetails.Id;
            Name = appResult.ImageDetails.Name;
            Description = appResult.ImageDetails.Description;
            Source = appResult.ImageDetails.Source;
            CardSide = appResult.CardSide;
            InitialUploadUtcDate = appResult.ImageDetails.InitialUploadUtcDate;
            InitialVersionCreator = appResult.ImageDetails.UploaderUserName;
            CurrentVersionUtcDate = appResult.ImageDetails.LastChangeUtcDate;
            CurrentVersionDescription = appResult.ImageDetails.VersionDescription;
            CardCount = appResult.ImageDetails.CardCount;
            OriginalImageContentType = appResult.ImageDetails.OriginalImageContentType;
            OriginalImageSize = appResult.ImageDetails.OriginalImageSize;
            SmallSize = appResult.ImageDetails.SmallSize;
            MediumSize = appResult.ImageDetails.MediumSize;
            BigSize = appResult.ImageDetails.BigSize;
        }
        public GetCardsImageViewModel(GetCardsToRepeat.ResultImageModel appResult)
        {
            ImageId = appResult.ImageDetails.Id;
            Name = appResult.ImageDetails.Name;
            Description = appResult.ImageDetails.Description;
            Source = appResult.ImageDetails.Source;
            CardSide = appResult.CardSide;
            InitialUploadUtcDate = appResult.ImageDetails.InitialUploadUtcDate;
            InitialVersionCreator = appResult.ImageDetails.UploaderUserName;
            CurrentVersionUtcDate = appResult.ImageDetails.LastChangeUtcDate;
            CurrentVersionDescription = appResult.ImageDetails.VersionDescription;
            CardCount = appResult.ImageDetails.CardCount;
            OriginalImageContentType = appResult.ImageDetails.OriginalImageContentType;
            OriginalImageSize = appResult.ImageDetails.OriginalImageSize;
            SmallSize = appResult.ImageDetails.SmallSize;
            MediumSize = appResult.ImageDetails.MediumSize;
            BigSize = appResult.ImageDetails.BigSize;
        }
        public GetCardsImageViewModel(GetCardsForDemo.ResultImageModel appResult)
        {
            ImageId = appResult.ImageDetails.Id;
            Name = appResult.ImageDetails.Name;
            Description = appResult.ImageDetails.Description;
            Source = appResult.ImageDetails.Source;
            CardSide = appResult.CardSide;
            InitialUploadUtcDate = appResult.ImageDetails.InitialUploadUtcDate;
            InitialVersionCreator = appResult.ImageDetails.UploaderUserName;
            CurrentVersionUtcDate = appResult.ImageDetails.LastChangeUtcDate;
            CurrentVersionDescription = appResult.ImageDetails.VersionDescription;
            CardCount = appResult.ImageDetails.CardCount;
            OriginalImageContentType = appResult.ImageDetails.OriginalImageContentType;
            OriginalImageSize = appResult.ImageDetails.OriginalImageSize;
            SmallSize = appResult.ImageDetails.SmallSize;
            MediumSize = appResult.ImageDetails.MediumSize;
            BigSize = appResult.ImageDetails.BigSize;
        }
        public Guid ImageId { get; }
        public string Name { get; }
        public string Description { get; }
        public string Source { get; }
        public int CardSide { get; }
        public DateTime InitialUploadUtcDate { get; }
        public string InitialVersionCreator { get; }
        public DateTime CurrentVersionUtcDate { get; }
        public string CurrentVersionDescription { get; }
        public int CardCount { get; }
        public string OriginalImageContentType { get; }
        public int OriginalImageSize { get; }
        public int SmallSize { get; }
        public int MediumSize { get; }
        public int BigSize { get; }
    }
    public sealed class GetCardsHeapModel
    {
        public GetCardsHeapModel(int heapId, string heapName, DateTime expiryUtcDate, ILocalized localizer)
        {
            HeapId = heapId;
            HeapName = heapName;
            if (heapId == 0)
            {
                ExpiryUtcDate = CardInDeck.NeverLearntLastLearnTime;
                MoveToAlertMessage = localizer.GetLocalized("MoveThisCardToHeap") + ' ' + heapName + " ?";
            }
            else
            {
                if (expiryUtcDate < DateTime.UtcNow)
                {
                    ExpiryUtcDate = CardInDeck.NeverLearntLastLearnTime;
                    MoveToAlertMessage = localizer.GetLocalized("MoveThisCardToHeap") + ' ' + heapName + " ? " + localizer.GetLocalized("ItWillBeExpired");
                }
                else
                {
                    ExpiryUtcDate = expiryUtcDate;
                    MoveToAlertMessage = localizer.GetLocalized("MoveThisCardToHeap") + ' ' + heapName + " ? " + localizer.GetLocalized("ItWillExpireOn");
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
    #region GetRemainingCardsInLesson
    [HttpPost("GetRemainingCardsInLesson")]
    public async Task<IActionResult> GetRemainingCardsInLessonAsync([FromBody] GetRemainingCardsInLessonRequest request)
    {
        CheckBodyParameter(request);
        var user = await userManager.GetUserAsync(HttpContext.User);
        var remainingCardsInLesson = await new GetRemainingCardsInLesson(callContext).RunAsync(new GetRemainingCardsInLesson.Request(user.Id, request.DeckId, request.LearnModeIsUnknown));
        var result = new GetRemainingCardsInLessonResult(remainingCardsInLesson.Count);
        return Ok(result);
    }
    #region Request and result classes
    public sealed class GetRemainingCardsInLessonRequest
    {
        public Guid DeckId { get; set; }
        public bool LearnModeIsUnknown { get; set; }
    }
    public sealed class GetRemainingCardsInLessonResult
    {
        public GetRemainingCardsInLessonResult(int remainingCardsInLesson)
        {
            RemainingCardsInLesson = remainingCardsInLesson;
        }
        public int RemainingCardsInLesson { get; }
    }
    #endregion
    #endregion
    #region UserDecks
    [HttpGet("UserDecks")]
    public async Task<IActionResult> UserDecks()
    {
        var user = await userManager.GetUserAsync(HttpContext.User);
        if (user == null)
            return Ok(new UserDecksViewModel(false, Array.Empty<UserDecksDeckViewModel>()));

        var decks = await new GetUserDecksWithTags(callContext).RunAsync(new GetUserDecksWithTags.Request(user.Id));
        var resultDecks = decks.Select(deck => new UserDecksDeckViewModel(deck.DeckId, deck.Description, DisplayServices.ShowDebugInfo(user), deck.Tags, this));
        var result = new UserDecksViewModel(true, resultDecks);
        return base.Ok(result);
    }
    public sealed class UserDecksViewModel
    {
        public UserDecksViewModel(bool userLoggedIn, IEnumerable<UserDecksDeckViewModel> decks)
        {
            UserLoggedIn = userLoggedIn;
            Decks = decks;
        }
        public bool UserLoggedIn { get; }
        public IEnumerable<UserDecksDeckViewModel> Decks { get; }
    }
    public sealed class UserDecksDeckViewModel
    {
        public UserDecksDeckViewModel(Guid deckId, string description, bool showDebugInfo, IEnumerable<GetUserDecksWithTags.ResultTag> tags, ILocalized localizer)
        {
            DeckId = deckId;
            Description = description;
            ShowDebugInfo = showDebugInfo;
            Tags = new[] { new UserDecksTagViewModel(noTagFakeGuid, localizer.GetLocalized("None")) }.Concat(tags.Select(tag => new UserDecksTagViewModel(tag.TagId, tag.TagName)));
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
            TagId = tagId;
            TagName = tagName;
        }
        public Guid TagId { get; }
        public string TagName { get; }
    }
    #endregion
    #region SetCardRating
    [HttpPatch("SetCardRating/{cardId}/{rating}")]
    public async Task<IActionResult> SetCardRating(Guid cardId, int rating)
    {
        var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
        var request = new SetCardRating.Request(userId, cardId, rating);
        await new SetCardRating(callContext).RunAsync(request);
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
            var request = new AddCardSubscriptions.Request(userId, cardId.AsArray());
            await new AddCardSubscriptions(callContext).RunAsync(request);
        }
        else
        {
            var request = new RemoveCardSubscriptions.Request(userId, cardId.AsArray());
            await new RemoveCardSubscriptions(callContext).RunAsync(request);
        }
        return Ok();
    }
    #endregion
}
