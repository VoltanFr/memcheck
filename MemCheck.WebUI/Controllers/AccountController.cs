using MemCheck.Application;
using MemCheck.Application.Notifying;
using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemCheck.WebUI.Controllers
{
    [Route("[controller]")]
    public class AccountController : MemCheckController
    {
        #region Fields
        private readonly CallContext callContext;
        private readonly UserManager<MemCheckUser> userManager;
        #endregion
        public AccountController(MemCheckDbContext dbContext, IStringLocalizer<AccountController> localizer, UserManager<MemCheckUser> userManager, TelemetryClient telemetryClient) : base(localizer)
        {
            callContext = new CallContext(dbContext, new MemCheckTelemetryClient(telemetryClient), this);
            this.userManager = userManager;
        }
        #region GetSearchSubscriptions
        [HttpPost("GetSearchSubscriptions")]
        public async Task<IActionResult> GetSearchSubscriptions()
        {
            var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
            var appRequest = new GetSearchSubscriptions.Request(userId);
            var result = await new GetSearchSubscriptions(callContext.DbContext).RunAsync(appRequest);
            return Ok(result.Select(appResultEntry => new SearchSubscriptionViewModel(appResultEntry, this)));
        }
        public sealed class SearchSubscriptionViewModel
        {
            public SearchSubscriptionViewModel(GetSearchSubscriptions.Result searchSubscription, ILocalized localizer)
            {
                Id = searchSubscription.Id;
                Name = searchSubscription.Name;
                var details = new StringBuilder();
                if (searchSubscription.ExcludedDeck != null)
                    details.Append(localizer.Get("ExcludedDeck") + ' ' + searchSubscription.ExcludedDeck + ", ");
                if (searchSubscription.RequiredText.Length > 0)
                    details.Append(localizer.Get("RequiredText") + " '" + searchSubscription.RequiredText + "', ");
                if (searchSubscription.RequiredTags.Count() == 1)
                    details.Append(localizer.Get("RequiredTag") + ' ' + string.Join(',', searchSubscription.RequiredTags) + ", ");
                if (searchSubscription.RequiredTags.Count() > 1)
                    details.Append(localizer.Get("RequiredTags") + ' ' + string.Join(',', searchSubscription.RequiredTags) + ", ");
                if (searchSubscription.ExcludeAllTags)
                    details.Append(localizer.Get("OnlyCardsWithNoTag") + ", ");
                else
                if (searchSubscription.ExcludedTags.Count() == 1)
                    details.Append(localizer.Get("ExcludedTag") + ' ' + string.Join(',', searchSubscription.ExcludedTags) + ", ");
                if (searchSubscription.ExcludedTags.Count() > 1)
                    details.Append(localizer.Get("ExcludedTags") + ' ' + string.Join(',', searchSubscription.ExcludedTags) + ", ");
                if (details.Length == 0)
                    details.Append(localizer.Get("AllCards"));
                Details = details.ToString();
                CardCountOnLastRun = searchSubscription.CardCountOnLastRun;
                RegistrationUtcDate = searchSubscription.RegistrationUtcDate;
                LastRunUtcDate = searchSubscription.LastRunUtcDate;
                DeleteConfirmMessage = localizer.Get("AreYouSureYouWantToDeleteTheSearch") + " '" + Name + "'";
            }
            public Guid Id { get; }
            public string Name { get; } = null!;
            public string Details { get; } = null!;
            public int CardCountOnLastRun { get; }
            public DateTime RegistrationUtcDate { get; }
            public DateTime LastRunUtcDate { get; }
            public string DeleteConfirmMessage { get; } = null!;
        }
        #endregion
        #region GetSearchSubscription
        [HttpGet("GetSearchSubscription/{Id}")]
        public async Task<IActionResult> GetSearchSubscription(Guid id)
        {
            var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
            var appRequest = new GetSearchSubscriptions.Request(userId);
            var results = await new GetSearchSubscriptions(callContext.DbContext).RunAsync(appRequest);  //Using this class is of course overkill, but it's ok since a user has very few search subscriptions
            var result = results.Where(r => r.Id == id).Single();
            return Ok(new SearchSubscriptionViewModel(result, this));
        }
        #endregion
        #region SetSearchSubscriptionName
        [HttpPut("SetSearchSubscriptionName/{Id}")]
        public async Task<IActionResult> SetSearchSubscriptionName(Guid id, [FromBody] SetSearchSubscriptionNameRequestModel request)
        {
            CheckBodyParameter(request);
            var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
            var appRequest = new SetSearchSubscriptionName.Request(userId, id, request.NewName);
            await new SetSearchSubscriptionName(callContext.DbContext).RunAsync(appRequest);
            return Ok();
        }
        public sealed class SetSearchSubscriptionNameRequestModel
        {
            public string NewName { get; set; } = null!;
        }
        #endregion
        #region DeleteSearchSubscription
        [HttpDelete("DeleteSearchSubscription/{Id}")]
        public async Task<IActionResult> DeleteSearchSubscription(Guid id)
        {
            var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
            var appRequest = new DeleteSearchSubscription.Request(userId, id);
            await new DeleteSearchSubscription(callContext).RunAsync(appRequest);
            return ControllerResultWithToast.Success(Get("Deleted"), this);
        }
        #endregion
    }
}
