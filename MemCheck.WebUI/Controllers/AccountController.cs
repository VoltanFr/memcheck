﻿using MemCheck.Application;
using MemCheck.Application.Notifying;
using MemCheck.Application.QueryValidation;
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
    public class AccountController : MemCheckController
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        private readonly UserManager<MemCheckUser> userManager;
        #endregion
        public AccountController(MemCheckDbContext dbContext, IStringLocalizer<AccountController> localizer, UserManager<MemCheckUser> userManager) : base(localizer)
        {
            this.dbContext = dbContext;
            this.userManager = userManager;
        }
        #region GetSearchSubscriptions
        [HttpPost("GetSearchSubscriptions")]
        public async Task<IActionResult> GetSearchSubscriptions()
        {
            var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
            var appRequest = new GetSearchSubscriptions.Request(userId);
            var result = await new GetSearchSubscriptions(dbContext).RunAsync(appRequest);
            return Ok(result.Select(appResultEntry => new SearchSubscriptionViewModel(appResultEntry, Localizer)));
        }
        public sealed class SearchSubscriptionViewModel
        {
            public SearchSubscriptionViewModel(GetSearchSubscriptions.Result searchSubscription, IStringLocalizer localizer)
            {
                Id = searchSubscription.Id;
                Name = searchSubscription.Name;
                var details = new StringBuilder();
                if (searchSubscription.ExcludedDeck != null)
                    details.Append(localizer["ExcludedDeck"].Value + ' ' + searchSubscription.ExcludedDeck + ", ");
                if (searchSubscription.RequiredText.Length > 0)
                    details.Append(localizer["RequiredText"].Value + " '" + searchSubscription.RequiredText + "', ");
                if (searchSubscription.RequiredTags.Count() == 1)
                    details.Append(localizer["RequiredTag"].Value + ' ' + string.Join(',', searchSubscription.RequiredTags) + ", ");
                if (searchSubscription.RequiredTags.Count() > 1)
                    details.Append(localizer["RequiredTags"].Value + ' ' + string.Join(',', searchSubscription.RequiredTags) + ", ");
                if (searchSubscription.ExcludeAllTags)
                    details.Append(localizer["OnlyCardsWithNoTag"].Value + ", ");
                else
                if (searchSubscription.ExcludedTags.Count() == 1)
                    details.Append(localizer["ExcludedTag"].Value + ' ' + string.Join(',', searchSubscription.ExcludedTags) + ", ");
                if (searchSubscription.ExcludedTags.Count() > 1)
                    details.Append(localizer["ExcludedTags"].Value + ' ' + string.Join(',', searchSubscription.ExcludedTags) + ", ");
                if (details.Length == 0)
                    details.Append(localizer["AllCards"].Value);
                Details = details.ToString();
                CardCountOnLastRun = searchSubscription.CardCountOnLastRun;
                RegistrationUtcDate = searchSubscription.RegistrationUtcDate;
                LastRunUtcDate = searchSubscription.LastRunUtcDate;
                DeleteConfirmMessage = localizer["AreYouSureYouWantToDeleteTheSearch"].Value + " '" + Name + "'";
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
            var results = await new GetSearchSubscriptions(dbContext).RunAsync(appRequest);  //Using this class is of course overkill, but it's ok since a user has very few search subscriptions
            var result = results.Where(r => r.Id == id).Single();
            return Ok(new SearchSubscriptionViewModel(result, Localizer));
        }
        #endregion
        #region SetSearchSubscriptionName
        [HttpPut("SetSearchSubscriptionName/{Id}")]
        public async Task<IActionResult> SetSearchSubscriptionName(Guid id, [FromBody] SetSearchSubscriptionNameRequestModel request)
        {
            var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
            var appRequest = new SetSearchSubscriptionName.Request(userId, id, request.NewName);
            await new SetSearchSubscriptionName(dbContext).RunAsync(appRequest);
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
            await new DeleteSearchSubscription(dbContext).RunAsync(appRequest);
            return base.Ok(Localizer["Deleted"].Value);
        }
        #endregion
    }
}
