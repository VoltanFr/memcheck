using MemCheck.Application;
using MemCheck.Application.QueryValidation;
using MemCheck.Application.Tags;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.WebUI.Controllers;

[Route("[controller]")]
public class TagsController : MemCheckController
{
    #region Fields
    private readonly CallContext callContext;
    private readonly UserManager<MemCheckUser> userManager;
    #endregion
    public TagsController(MemCheckDbContext dbContext, UserManager<MemCheckUser> userManager, IStringLocalizer<TagsController> localizer, TelemetryClient telemetryClient) : base(localizer)
    {
        callContext = new CallContext(dbContext, new MemCheckTelemetryClient(telemetryClient), this, new ProdRoleChecker(userManager));
        this.userManager = userManager;
    }
    #region GetGuiMessages
    [HttpGet("GetGuiMessages")]
    public IActionResult GetGuiMessages()
    {
        return Ok(new GetGuiMessagesViewModel(GetLocalized("AlreadyExistsErrMesg"), GetLocalized("NameLengthErrMesg")));
    }
    public sealed class GetGuiMessagesViewModel
    {
        public GetGuiMessagesViewModel(string alreadyExistsErr, string nameLengthErr)
        {
            AlreadyExistsErr = alreadyExistsErr;
            NameLengthErr = nameLengthErr;
        }
        public string AlreadyExistsErr { get; } = null!;
        public string NameLengthErr { get; } = null!;
    }
    #endregion
    #region GetTag
    [HttpGet("GetTag/{tagId}")]
    public async Task<IActionResult> GetTag(Guid tagId)
    {
        var result = await new GetTag(callContext).RunAsync(new GetTag.Request(tagId));
        return Ok(new GetTagViewModel(result));
    }
    public sealed class GetTagViewModel
    {
        public GetTagViewModel(GetTag.Result tag)
        {
            TagId = tag.TagId;
            TagName = tag.TagName;
            Description = tag.Description;
            CardCount = tag.CardCount;
            VersionCreatorName = tag.CreatingUserName;
            VersionUtcDate = tag.VersionUtcDate;
        }
        public Guid TagId { get; }
        public string TagName { get; }
        public string Description { get; }
        public int CardCount { get; }
        public string VersionCreatorName { get; }
        public DateTime VersionUtcDate { get; }
    }
    #endregion
    #region GetTags
    [HttpPost("GetTags")]
    public async Task<IActionResult> GetTagsAsync([FromBody] GetTagsRequest request)
    {
        CheckBodyParameter(request);
        var user = await userManager.GetUserAsync(HttpContext.User);
        var result = await new GetAllTags(callContext).RunAsync(new GetAllTags.Request(request.PageSize, request.PageNo, request.Filter));
        return Ok(new GetTagsViewModel(result, user != null));
    }
    public sealed class GetTagsRequest
    {
        public int PageSize { get; set; }
        public int PageNo { get; set; }
        public string Filter { get; set; } = null!;
    }
    public sealed class GetTagsViewModel
    {
        public GetTagsViewModel(GetAllTags.Result applicationResult, bool userLoggedIn)
        {
            UserLoggedIn = userLoggedIn;
            TotalCount = applicationResult.TotalCount;
            PageCount = applicationResult.PageCount;
            Tags = applicationResult.Tags.Select(tag => new GetTagsTagViewModel(tag));
        }
        public bool UserLoggedIn { get; }
        public int TotalCount { get; }
        public int PageCount { get; }
        public IEnumerable<GetTagsTagViewModel> Tags { get; }
    }
    public sealed class GetTagsTagViewModel
    {
        public GetTagsTagViewModel(GetAllTags.ResultTag tag)
        {
            TagId = tag.TagId;
            TagName = tag.TagName;
            TagDescription = tag.TagDescription;
            CardCount = tag.CardCount;
            AverageRating = tag.AverageRating;
        }
        public Guid TagId { get; }
        public string TagName { get; } = null!;
        public string TagDescription { get; } = null!;
        public string CreatingUserName { get; } = null!;
        public int CardCount { get; }
        public double AverageRating { get; }
    }
    #endregion
    #region GetTagNames
    [HttpGet("GetTagNames")]
    public async Task<IActionResult> GetTagNamesAsync()
    {
        var result = await new GetAllTags(callContext).RunAsync(new GetAllTags.Request(GetAllTags.Request.MaxPageSize, 1, ""));
        return Ok(result.Tags.Select(tag => tag.TagName));
    }
    #endregion
    #region Create
    [HttpPost("Create"), Authorize]
    public async Task<IActionResult> Create([FromBody] CreateRequestModel request)
    {
        CheckBodyParameter(request);
        var user = await UserServices.UserFromContextAsync(HttpContext, userManager);
        await new CreateTag(callContext).RunAsync(new CreateTag.Request(user.Id, request.NewName.Trim(), request.NewDescription.Trim(), "Initial version"));
        return ControllerResultWithToast.Success(GetLocalized("TagRecorded") + ' ' + request.NewName, this);

    }
    public sealed class CreateRequestModel
    {
        public string NewName { get; set; } = null!;
        public string NewDescription { get; set; } = null!;
    }
    #endregion
    #region Update
    [HttpPut("Update/{tagId}"), Authorize]
    public async Task<IActionResult> Update(Guid tagId, [FromBody] UpdateRequestModel request)
    {
        CheckBodyParameter(request);
        var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
        await new UpdateTag(callContext).RunAsync(new UpdateTag.Request(userId, tagId, request.NewName.Trim(), request.NewDescription.Trim(), "Update"));
        return ControllerResultWithToast.Success(GetLocalized("TagRecorded") + ' ' + request.NewName, this);

    }
    public sealed class UpdateRequestModel
    {
        public string NewName { get; set; } = null!;
        public string NewDescription { get; set; } = null!;
    }
    #endregion
}
