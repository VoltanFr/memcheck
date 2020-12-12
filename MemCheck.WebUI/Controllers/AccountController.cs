using MemCheck.Application;
using MemCheck.Application.Notifying;
using MemCheck.Database;
using MemCheck.Domain;
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
    [Route("[controller]")]
    public class AccountController : Controller, ILocalized
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        private readonly UserManager<MemCheckUser> userManager;
        private readonly IStringLocalizer<AccountController> localizer;
        #endregion
        public AccountController(MemCheckDbContext dbContext, IStringLocalizer<AccountController> localizer, UserManager<MemCheckUser> userManager) : base()
        {
            this.dbContext = dbContext;
            this.localizer = localizer;
            this.userManager = userManager;
        }
        public IStringLocalizer Localizer
        {
            get
            {
                return localizer;
            }
        }
        #region GetSearchSubscriptions
        [HttpPost("GetSearchSubscriptions")]
        public async Task<IActionResult> GetSearchSubscriptions()
        {
            try
            {
                var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
                var appRequest = new GetSearchSubscriptions.Request(userId);
                var result = await new GetSearchSubscriptions(dbContext).RunAsync(appRequest);
                return Ok(result.Select(appResultEntry => new SearchSubscriptionViewModel(appResultEntry, localizer)));
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
        public sealed class SearchSubscriptionViewModel
        {
            #region Fields
            private readonly IStringLocalizer<AccountController> localizer;
            #endregion
            public SearchSubscriptionViewModel(GetSearchSubscriptions.Result searchSubscription, IStringLocalizer<AccountController> localizer)
            {
                Id = searchSubscription.Id;
                Name = searchSubscription.Name;
                var details = new StringBuilder();
                if (searchSubscription.ExcludedDeck != null)
                    details.Append(localizer["ExcludedDeck"].Value + ' ' + searchSubscription.ExcludedDeck + "<br/>");
                if (searchSubscription.RequiredText.Length > 0)
                    details.Append(localizer["RequiredText"].Value + ' ' + searchSubscription.RequiredText + "<br/>");
                if (searchSubscription.RequiredTags.Any())
                    details.Append(localizer["RequiredTags"].Value + ' ' + string.Join(',', searchSubscription.RequiredTags) + "<br/>");
                if (searchSubscription.ExcludeAllTags)
                    details.Append(localizer["OnlyCardsWithNoTag"].Value + "<br/>");
                else
                if (searchSubscription.ExcludedTags.Any())
                    details.Append(localizer["ExcludedTags"].Value + ' ' + string.Join(',', searchSubscription.ExcludedTags) + "<br/>");
                if (details.Length == 0)
                    details.Append(localizer["AllCards"].Value);
                Details = details.ToString();
                CardCountOnLastRun = searchSubscription.CardCountOnLastRun;
                RegistrationUtcDate = searchSubscription.RegistrationUtcDate;
                LastRunUtcDate = searchSubscription.LastRunUtcDate;
                this.localizer = localizer;
            }
            public Guid Id { get; }
            public string Name { get; } = null!;
            public string Details { get; } = null!;
            public int CardCountOnLastRun { get; }
            public DateTime RegistrationUtcDate { get; }
            public DateTime LastRunUtcDate { get; }
        }
        #endregion
        #region GetSearchSubscription
        [HttpGet("GetSearchSubscription/{Id}")]
        public async Task<IActionResult> GetSearchSubscription(Guid id)
        {
            try
            {
                var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
                var appRequest = new GetSearchSubscriptions.Request(userId);
                var results = await new GetSearchSubscriptions(dbContext).RunAsync(appRequest);  //Using this class is of course overkill, but it's ok since a user has very few search subscriptions
                var result = results.Where(r => r.Id == id).Single();
                return Ok(new SearchSubscriptionViewModel(result, localizer));
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
        #endregion
        #region Update
        [HttpPut("SetSearchSubscriptionName/{Id}")]
        public async Task<IActionResult> SetSearchSubscriptionName(Guid id, [FromBody] SetSearchSubscriptionNameRequestModel request)
        {
            try
            {
                var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
                var appRequest = new SetSearchSubscriptionName.Request(userId, id, request.NewName);
                await new SetSearchSubscriptionName(dbContext).RunAsync(appRequest);
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
        public sealed class SetSearchSubscriptionNameRequestModel
        {
            public string NewName { get; set; } = null!;
        }
        #endregion
    }
}
