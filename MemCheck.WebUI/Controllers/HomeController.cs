using MemCheck.Application.Decks;
using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using MemCheck.Application;
using MemCheck.Application.Tags;

namespace MemCheck.WebUI.Controllers;

[Route("[controller]")]
public class HomeController : MemCheckController
{
    #region Fields
    private readonly CallContext callContext;
    private readonly UserManager<MemCheckUser> userManager;
    #endregion
    public HomeController(MemCheckDbContext dbContext, UserManager<MemCheckUser> userManager, IStringLocalizer<HomeController> localizer, TelemetryClient telemetryClient) : base(localizer)
    {
        callContext = new CallContext(dbContext, new MemCheckTelemetryClient(telemetryClient), this, new ProdRoleChecker(userManager));
        this.userManager = userManager;
    }
    #region GetAll
    [HttpGet("GetAll")]
    public async Task<IActionResult> GetAllAsync()
    {
        var user = await userManager.GetUserAsync(HttpContext.User);
        if (user == null)
        {
            var recommendedTagsForDemo = await new GetRecommendedTagsForDemo(callContext).RunAsync(new GetRecommendedTagsForDemo.Request(100, 4.7));
            return Ok(new GetAllResult(null, false, 0, Array.Empty<GetAllResult_Deck>(), DateTime.UtcNow, recommendedTagsForDemo.Tags.Select(resultTag => new GetAllResult_Tag(resultTag.TagId, resultTag.TagName))));
        }

        var userDecks = await new GetDecksWithLearnCounts(callContext).RunAsync(new GetDecksWithLearnCounts.Request(user.Id));
        var anythingToLearn = userDecks.Any(deck => deck.ExpiredCardCount > 0 || deck.UnknownCardCount > 0);
        var cardCount = userDecks.Sum(deck => deck.CardCount);

        return Ok(new GetAllResult(user.UserName, anythingToLearn, cardCount, userDecks.Select(deck => new GetAllResult_Deck(deck, userDecks.Length == 1, this)), DateTime.UtcNow, Array.Empty<GetAllResult_Tag>()));
    }
    #region Result classes
    public sealed class GetAllResult
    {
        private static TimeSpan GetReloadWaitTime(IEnumerable<GetAllResult_Deck> userDecks)
        {
            if (!userDecks.Any())
                //Refreshing is useless
                return TimeSpan.Zero;

            if (userDecks.Any(deck => deck.ExpiredCardCount > 0))
                return TimeSpan.FromMinutes(10);

            var sleepTimeForDecks = userDecks.Min(deck => deck.ExpiredCardCount > 0 ? TimeSpan.FromMinutes(10) : deck.NextExpiryUTCDate - DateTime.UtcNow);
            return new[] { TimeSpan.FromMinutes(1), sleepTimeForDecks }.Max().Add(TimeSpan.FromMinutes(1));    //Not less than 2'
        }
        public GetAllResult(string? userName, bool anythingToLearn, int totalCardCountInDecksOfUser, IEnumerable<GetAllResult_Deck> userDecks, DateTime dataUTCDate, IEnumerable<GetAllResult_Tag> recommendedTagsForDemo)
        {
            UserName = userName;
            AnythingToLearn = anythingToLearn;
            TotalCardCountInDecksOfUser = totalCardCountInDecksOfUser;
            //Formula below: if a deck is expired, it will not make us refresh before 10'. And anyway we won't refresh before 2'
            ReloadWaitTime = (int)GetReloadWaitTime(userDecks).TotalMilliseconds;
            UserDecks = userDecks;
            DataUTCDate = dataUTCDate;
            RecommendedTagsForDemo = recommendedTagsForDemo;
        }
        public string? UserName { get; }    //null if no user
        public bool AnythingToLearn { get; }
        public int TotalCardCountInDecksOfUser { get; }
        public int ReloadWaitTime { get; }  //milliseconds
        public IEnumerable<GetAllResult_Deck> UserDecks { get; }
        public DateTime DataUTCDate { get; }
        public IEnumerable<GetAllResult_Tag> RecommendedTagsForDemo { get; } //Empty if a user is logged in
    }
    public sealed class GetAllResult_Deck
    {
        public GetAllResult_Deck(GetDecksWithLearnCounts.Result applicationDeck, bool isTheOnlyDeck, ILocalized localizer)
        {
            NextExpiryUTCDate = applicationDeck.NextExpiryUTCDate;

            var lines = new List<string>();
            if (applicationDeck.CardCount == 0)
            {
                HeadLine = localizer.GetLocalized("ThereIsNoCardInYourDeck") + $" <a href=\"/Decks/Index?DeckId={applicationDeck.Id}\">{applicationDeck.Description}</a>.";
                lines.Add($"<a href=\"/Search/Index\" >{localizer.GetLocalized("ClickHereToSearchAndAddCards")}</a>...");
                lines.Add($"<a href=\"/Authoring/Index\">{localizer.GetLocalized("ClickHereToCreateCards")}</a>...");
            }
            else
            {
                HeadLine = isTheOnlyDeck
                    ? $"{localizer.GetLocalized("AmongThe")} {applicationDeck.CardCount} {localizer.GetLocalized("CardsOf")} <a href=\"/Decks/Index?DeckId={applicationDeck.Id}\">{localizer.GetLocalized("YourDeck")}</a>..."
                    : $"{localizer.GetLocalized("AmongThe")} {applicationDeck.CardCount} {localizer.GetLocalized("CardsOf")} {localizer.GetLocalized("YourDeck")} <a href=\"/Decks/Index?DeckId={applicationDeck.Id}\">{applicationDeck.Description}</a>...";
                if (applicationDeck.UnknownCardCount == 0)
                    lines.Add(localizer.GetLocalized("NoUnknownCard"));
                else
                {
                    var linkText = applicationDeck.UnknownCardCount == 1 ? localizer.GetLocalized("OneUnknownCard") : $"{applicationDeck.UnknownCardCount} {localizer.GetLocalized("UnknownCards")}";
                    lines.Add($"<a href=\"/Learn/Index?LearnMode=Unknown\">{linkText}</a>");
                }
                if (applicationDeck.ExpiredCardCount == 0)
                    lines.Add(localizer.GetLocalized("NoExpiredCard"));
                else
                {
                    var linkText = applicationDeck.ExpiredCardCount == 1 ? localizer.GetLocalized("OneExpiredCard") : $"{applicationDeck.ExpiredCardCount} {localizer.GetLocalized("ExpiredCards")}";
                    lines.Add($"<a href=\"/Learn/Index?LearnMode=Expired\">{linkText}</a>");
                }
                if (applicationDeck.ExpiringNextHourCount == 0)
                    lines.Add(localizer.GetLocalized("NoCardToExpireInTheNextHour"));
                else
                {
                    if (applicationDeck.ExpiringNextHourCount == 1)
                        lines.Add(localizer.GetLocalized("OneCardWillExpireInTheNextHour"));
                    else
                        lines.Add($"{applicationDeck.ExpiringNextHourCount} {localizer.GetLocalized("CardsWillExpireInTheNextHour")}");
                }
                if (applicationDeck.ExpiringFollowing24hCount == 0)
                    lines.Add(localizer.GetLocalized("NoCardToExpireInTheFollowing24h"));
                else
                {
                    if (applicationDeck.ExpiringFollowing24hCount == 1)
                        lines.Add(localizer.GetLocalized("OneCardWillExpireInTheFollowing24h"));
                    else
                        lines.Add($"{applicationDeck.ExpiringFollowing24hCount} {localizer.GetLocalized("CardsWillExpireInTheFollowing24h")}");
                }
                if (applicationDeck.ExpiringFollowing3DaysCount == 0)
                    lines.Add(localizer.GetLocalized("NoCardToExpireInTheFollowing3Days"));
                else
                {
                    if (applicationDeck.ExpiringFollowing3DaysCount == 1)
                        lines.Add(localizer.GetLocalized("OneCardWillExpireInTheFollowing3Days"));
                    else
                        lines.Add($"{applicationDeck.ExpiringFollowing3DaysCount} {localizer.GetLocalized("CardsWillExpireInTheFollowing3Days")}");
                }
            }
            Lines = lines;
        }
        internal int ExpiredCardCount { get; }
        public DateTime NextExpiryUTCDate { get; }
        public string HeadLine { get; }
        public IEnumerable<string> Lines { get; }
    }
    public sealed record GetAllResult_Tag(Guid TagId, string TagName);
    #endregion
    #endregion
}
