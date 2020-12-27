using MemCheck.Application.Loading;
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

namespace MemCheck.WebUI.Controllers
{
    [Route("[controller]")]
    public class HomeController : MemCheckController
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        private readonly UserManager<MemCheckUser> userManager;
        #endregion
        public HomeController(MemCheckDbContext dbContext, UserManager<MemCheckUser> userManager, IStringLocalizer<HomeController> localizer) : base(localizer)
        {
            this.dbContext = dbContext;
            this.userManager = userManager;
        }
        #region GetAll
        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAllAsync()
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            if (user == null)
                return Ok(new GetAllViewModel(null, false, 0, new GetAllDeckViewModel[0], DateTime.UtcNow));

            var userDecks = new GetDecksWithLearnCounts(dbContext).Run(user.Id);
            var anythingToLearn = userDecks.Any(deck => deck.ExpiredCardCount > 0 || deck.UnknownCardCount > 0);
            var cardCount = userDecks.Sum(deck => deck.CardCount);

            return Ok(new GetAllViewModel(user.UserName, anythingToLearn, cardCount, userDecks.Select(deck => new GetAllDeckViewModel(deck, this)), DateTime.UtcNow));
        }
        #region Result classes
        public sealed class GetAllViewModel
        {
            private TimeSpan GetReloadWaitTime(IEnumerable<GetAllDeckViewModel> userDecks)
            {
                if (!userDecks.Any())
                    //Refreshing is useless
                    return TimeSpan.Zero;

                if (userDecks.Any(deck => deck.ExpiredCardCount > 0))
                    return TimeSpan.FromMinutes(10);

                var sleepTimeForDecks = userDecks.Select(deck => deck.ExpiredCardCount > 0 ? TimeSpan.FromMinutes(10) : deck.NextExpiryUTCDate - DateTime.UtcNow).Min();
                return new[] { TimeSpan.FromMinutes(1), sleepTimeForDecks }.Max().Add(TimeSpan.FromMinutes(1));    //Not less than 2'
            }
            public GetAllViewModel(string? userName, bool anythingToLearn, int totalCardCountInDecksOfUser, IEnumerable<GetAllDeckViewModel> userDecks, DateTime dataUTCDate)
            {
                UserName = userName;
                AnythingToLearn = anythingToLearn;
                TotalCardCountInDecksOfUser = totalCardCountInDecksOfUser;
                //Formula below: if a deck is expired, it will not make us refresh before 10'. And anyway we won't refresh before 2'
                ReloadWaitTime = (int)GetReloadWaitTime(userDecks).TotalMilliseconds;
                UserDecks = userDecks;
                DataUTCDate = dataUTCDate;
            }
            public string? UserName { get; }    //null if no user
            public bool AnythingToLearn { get; }
            public int TotalCardCountInDecksOfUser { get; }
            public int ReloadWaitTime { get; }  //milliseconds
            public IEnumerable<GetAllDeckViewModel> UserDecks { get; }
            public DateTime DataUTCDate { get; }
        }
        public sealed class GetAllDeckViewModel
        {
            public GetAllDeckViewModel(GetDecksWithLearnCounts.Result applicationDeck, ILocalized localizer)
            {
                var DeckId = applicationDeck.Id;
                var UnknownCardCount = applicationDeck.UnknownCardCount;
                var ExpiredCardCount = applicationDeck.ExpiredCardCount;
                var Description = applicationDeck.Description;
                NextExpiryUTCDate = applicationDeck.NextExpiryUTCDate;

                HeadLine = localizer.Get("InYourDeck") + $" <a href=\"/Decks/Index?DeckId={DeckId}\">{Description}</a>...";
                var lines = new List<string>();
                if (applicationDeck.IsEmpty)
                {
                    lines.Add(localizer.Get("ThereIsNoCard"));
                    lines.Add($"<a href=\"/Search/Index\" >{localizer.Get("ClickHereToSearchAndAddCards")}</a>...");
                    lines.Add($"<a href=\"/Authoring/Index\">{localizer.Get("ClickHereToCreateCards")}</a>...");
                }
                else
                {
                    if (UnknownCardCount == 0)
                        lines.Add(localizer.Get("NoUnknownCard") + '.');
                    else
                    {
                        var linkText = UnknownCardCount == 1 ? localizer.Get("OneUnknownCard") : $"{UnknownCardCount} {localizer.Get("UnknownCards")}";
                        lines.Add($"<a href=\"/Learn/Index?LearnMode=Unknown\">{linkText}</a>");
                    }
                    if (ExpiredCardCount == 0)
                        lines.Add(localizer.Get("NoExpiredCard") + '.');
                    else
                    {
                        var linkText = ExpiredCardCount == 1 ? localizer.Get("OneExpiredCard") : $"{ExpiredCardCount} {localizer.Get("ExpiredCards")}";
                        lines.Add($"<a href=\"/Learn/Index?LearnMode=Expired\">{linkText}</a>");
                    }
                }
                Lines = lines;
            }
            internal int ExpiredCardCount { get; }
            internal DateTime NextExpiryUTCDate { get; }  //meaningless if ExpiredCardCount > 0
            public string HeadLine { get; }
            public IEnumerable<string> Lines { get; }
        }
        #endregion
        #endregion
    }
}
