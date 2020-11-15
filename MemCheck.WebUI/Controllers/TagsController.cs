using MemCheck.Application;
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
    public class TagsController : Controller
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        private readonly IStringLocalizer<TagsController> localizer;
        #endregion
        public TagsController(MemCheckDbContext dbContext, IStringLocalizer<TagsController> localizer) : base()
        {
            this.dbContext = dbContext;
            this.localizer = localizer;
        }
        #region GetGuiMessages
        [HttpGet("GetGuiMessages")]
        public IActionResult GetGuiMessages()
        {
            return Ok(new GetGuiMessagesViewModel(
                localizer["AlreadyExistsErrMesg"].Value,
                localizer["NameLengthErrMesg"].Value,
                localizer["Saved"].Value,
                localizer["LabelName"].Value));
        }
        public sealed class GetGuiMessagesViewModel
        {
            public GetGuiMessagesViewModel(string alreadyExistsErr, string nameLengthErr, string saved, string labelName)
            {
                this.alreadyExistsErr = alreadyExistsErr;
                this.nameLengthErr = nameLengthErr;
                this.saved = saved;
                this.labelName = labelName;
            }
            public string alreadyExistsErr { get; } = null!;
            public string nameLengthErr { get; } = null!;
            public string saved { get; } = null!;
            public string labelName { get; } = null!;
        }
        #endregion
        #region GetTag
        [HttpGet("GetTag/{tagId}")]
        public IActionResult GetTag(Guid tagId)
        {
            var result = new GetTag(dbContext).Run(tagId);
            return Ok(new GetTagViewModel(result));
        }
        public sealed class GetTagViewModel
        {
            public GetTagViewModel(GetTag.ResultModel tag)
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
        #region GetTags
        [HttpPost("GetTags")]
        public IActionResult GetTags([FromBody] GetTagsRequest request)
        {
            var result = new GetAllTags(dbContext).Run(request.PageSize, request.PageNo, request.Filter);
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
            public GetTagsViewModel(GetAllTags.ResultModel applicationResult)
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
            public GetTagsTagViewModel(GetAllTags.ResultTagModel tag)
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
        public IActionResult GetTagNames()
        {
            var result = new GetAllTagNames(dbContext).Run();
            return Ok(result);
        }
        #endregion
        #region Create
        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] CreateRequestModel request)
        {
            return Ok(await new CreateTag(dbContext).RunAsync(request.NewName));
        }
        public sealed class CreateRequestModel
        {
            public string NewName { get; set; } = null!;
        }
        #endregion
        #region Update
        [HttpPut("Update/{tagId}")]
        public async Task<IActionResult> Update(Guid tagId, [FromBody] UpdateRequestModel request)
        {
            return Ok(await new UpdateTag(dbContext).RunAsync(tagId, request.NewName));
        }
        public sealed class UpdateRequestModel
        {
            public string NewName { get; set; } = null!;
        }
        #endregion
    }
}
