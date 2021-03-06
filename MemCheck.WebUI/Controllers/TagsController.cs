﻿using MemCheck.Application.Tags;
using MemCheck.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.WebUI.Controllers
{
    [Route("[controller]")]
    public class TagsController : MemCheckController
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public TagsController(MemCheckDbContext dbContext, IStringLocalizer<TagsController> localizer) : base(localizer)
        {
            this.dbContext = dbContext;
        }
        #region GetGuiMessages
        [HttpGet("GetGuiMessages")]
        public IActionResult GetGuiMessages()
        {
            return Ok(new GetGuiMessagesViewModel(Get("AlreadyExistsErrMesg"), Get("NameLengthErrMesg")));
        }
        public sealed class GetGuiMessagesViewModel
        {
            public GetGuiMessagesViewModel(string alreadyExistsErr, string nameLengthErr)
            {
                this.AlreadyExistsErr = alreadyExistsErr;
                this.NameLengthErr = nameLengthErr;
            }
            public string AlreadyExistsErr { get; } = null!;
            public string NameLengthErr { get; } = null!;
        }
        #endregion
        #region GetTag
        [HttpGet("GetTag/{tagId}")]
        public async Task<IActionResult> GetTag(Guid tagId)
        {
            var result = await new GetTag(dbContext).RunAsync(new GetTag.Request(tagId));
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
            }
            public Guid TagId { get; }
            public string TagName { get; }
            public string Description { get; }
            public int CardCount { get; }
        }
        #endregion
        #region GetTags
        [HttpPost("GetTags")]
        public async Task<IActionResult> GetTagsAsync([FromBody] GetTagsRequest request)
        {
            CheckBodyParameter(request);
            var result = await new GetAllTags(dbContext).RunAsync(new GetAllTags.Request(request.PageSize, request.PageNo, request.Filter));
            return Ok(new GetTagsViewModel(result));
        }
        public sealed class GetTagsRequest
        {
            public int PageSize { get; set; }
            public int PageNo { get; set; }
            public string Filter { get; set; } = null!;
        }
        public sealed class GetTagsViewModel
        {
            public GetTagsViewModel(GetAllTags.Result applicationResult)
            {
                TotalCount = applicationResult.TotalCount;
                PageCount = applicationResult.PageCount;
                Tags = applicationResult.Tags.Select(tag => new GetTagsTagViewModel(tag));
            }
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
                CardCount = tag.CardCount;
            }
            public Guid TagId { get; }
            public string TagName { get; } = null!;
            public int CardCount { get; }
        }
        #endregion
        #region GetTagNames
        [HttpGet("GetTagNames")]
        public async Task<IActionResult> GetTagNamesAsync()
        {
            var result = await new GetAllTags(dbContext).RunAsync(new GetAllTags.Request(GetAllTags.Request.MaxPageSize, 1, ""));
            return Ok(result.Tags.Select(tag => tag.TagName));
        }
        #endregion
        #region Create
        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] CreateRequestModel request)
        {
            CheckBodyParameter(request);
            await new CreateTag(dbContext).RunAsync(new CreateTag.Request(request.NewName.Trim(), request.NewDescription.Trim()), this);
            return ControllerResultWithToast.Success(Get("TagRecorded") + ' ' + request.NewName, this);

        }
        public sealed class CreateRequestModel
        {
            public string NewName { get; set; } = null!;
            public string NewDescription { get; set; } = null!;
        }
        #endregion
        #region Update
        [HttpPut("Update/{tagId}")]
        public async Task<IActionResult> Update(Guid tagId, [FromBody] UpdateRequestModel request)
        {
            CheckBodyParameter(request);
            await new UpdateTag(dbContext).RunAsync(new UpdateTag.Request(tagId, request.NewName.Trim(), request.NewDescription.Trim()), this);
            return ControllerResultWithToast.Success(Get("TagRecorded") + ' ' + request.NewName, this);

        }
        public sealed class UpdateRequestModel
        {
            public string NewName { get; set; } = null!;
            public string NewDescription { get; set; } = null!;
        }
        #endregion
    }
}
