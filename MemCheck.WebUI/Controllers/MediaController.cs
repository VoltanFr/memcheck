using MemCheck.Application;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.WebUI.Controllers
{
    [Route("[controller]"), Authorize]
    public class MediaController : Controller, ILocalized
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        private readonly UserManager<MemCheckUser> userManager;
        private readonly IStringLocalizer<MediaController> localizer;
        #endregion
        public MediaController(MemCheckDbContext dbContext, UserManager<MemCheckUser> userManager, IStringLocalizer<MediaController> localizer) : base()
        {
            this.dbContext = dbContext;
            this.userManager = userManager;
            this.localizer = localizer;
        }
        public IStringLocalizer Localizer => localizer;
        #region UploadImage
        [HttpPost("UploadImage")]
        public async Task<IActionResult> UploadImage([FromForm] UploadImageRequest request)
        {
            if (request.Name == null)
                return ControllerResult.FailureWithResourceMesg("NameNotSet", this);
            if (request.Description == null)
                return ControllerResult.FailureWithResourceMesg("DescriptionNotSet", this);
            if (request.Source == null)
                return ControllerResult.FailureWithResourceMesg("SourceNotSet", this);
            if (request.File == null)
                return ControllerResult.FailureWithResourceMesg("FileNotSet", this);

            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                if (user == null)
                    return ControllerResult.FailureWithResourceMesg("NeedLogin", this);

                using (var stream = request.File.OpenReadStream())
                using (var reader = new BinaryReader(stream))
                {
                    var fileContent = reader.ReadBytes((int)request.File.Length);
                    var applicationRequest = new StoreImage.Request(user, request.Name, request.Description, request.Source, request.File.ContentType, fileContent);
                    var id = await new StoreImage(dbContext, localizer).RunAsync(applicationRequest);
                    if (id == Guid.Empty)
                        throw new ApplicationException("Stored image with empty GUID as id");
                    return ControllerResult.Success(localizer["ImageSavedWithName"].Value + $" '{applicationRequest.Name}'", this);
                }
            }
            catch (Exception e)
            {
                return ControllerResult.Failure(e, this);
            }
        }
        public sealed class UploadImageRequest
        {
            public string? Name { get; set; }
            public string? Description { get; set; }
            public string? Source { get; set; }
            public IFormFile? File { get; set; }
        }
        #endregion
        #region GetImageList
        [HttpPost("GetImageList")]
        public IActionResult GetImageList([FromBody] GetImageListRequest request)
        {
            try
            {
                var result = new GetImageList(dbContext).Run(request.PageSize, request.PageNo, request.Filter ?? "");
                return Ok(new GetImageListViewModel(result, localizer));
            }
            catch (Exception e)
            {
                return ControllerError.BadRequest(e, this);
            }
        }
        public sealed class GetImageListRequest
        {
            public int PageSize { get; set; }
            public int PageNo { get; set; }
            public string? Filter { get; set; }
        }
        public sealed class GetImageListViewModel
        {
            public GetImageListViewModel(GetImageList.ResultModel applicationResult, IStringLocalizer localizer)
            {
                TotalCount = applicationResult.TotalCount;
                PageCount = applicationResult.PageCount;
                Images = applicationResult.Images.Select(img => new GetImageListImageViewModel(img, localizer));
            }
            public int TotalCount { get; }
            public int PageCount { get; }
            public IEnumerable<GetImageListImageViewModel> Images { get; }
        }
        public sealed class GetImageListImageViewModel
        {
            public GetImageListImageViewModel(GetImageList.ResultImageModel img, IStringLocalizer localizer)
            {
                ImageId = img.ImageId;
                ImageName = img.ImageName;
                CardCount = img.CardCount;
                UploaderUserName = img.Uploader;
                Description = img.Description;
                Source = img.Source;
                OriginalImageContentType = img.OriginalImageContentType;
                OriginalImageSize = img.OriginalImageSize;
                SmallSize = img.SmallSize;
                MediumSize = img.MediumSize;
                BigSize = img.BigSize;
                InitialUploadUtcDate = img.InitialUploadUtcDate;
                LastChangeUtcDate = img.LastChangeUtcDate;
                RemoveAlertMessage = $"{localizer["SureYouWantToDeletePart1"].Value} '{ImageName}' ? {localizer["SureYouWantToDeletePart2"].Value} {UploaderUserName} {localizer["SureYouWantToDeletePart3"]} ";
                currentVersionDescription = img.CurrentVersionDescription;
            }
            public Guid ImageId { get; }
            public string ImageName { get; }
            public int CardCount { get; }
            public string UploaderUserName { get; }
            public string Description { get; }
            public string Source { get; }
            public string OriginalImageContentType { get; }
            public int OriginalImageSize { get; }
            public int SmallSize { get; }
            public int MediumSize { get; }
            public int BigSize { get; }
            public DateTime InitialUploadUtcDate { get; }
            public string RemoveAlertMessage { get; }
            public DateTime LastChangeUtcDate { get; }
            public string currentVersionDescription { get; }
        }
        #endregion
        #region GetImageMetadata
        [HttpGet("GetImageMetadata/{imageId}")]
        public async Task<IActionResult> GetImageMetadata(Guid imageId)
        {
            try
            {
                var appRequest = new GetImageInfo(dbContext, localizer);
                var result = await appRequest.RunAsync(imageId);
                return Ok(new GetImageMetadataViewModel(result.Name, result.Source, result.Description));
            }
            catch (Exception e)
            {
                return ControllerError.BadRequest(e, this);
            }
        }
        public sealed class GetImageMetadataViewModel
        {
            public GetImageMetadataViewModel(string imageName, string source, string description)
            {
                this.imageName = imageName;
                this.source = source;
                this.description = description;
            }
            public string imageName { get; }
            public string source { get; }
            public string description { get; }
        }
        #endregion
        #region Update
        [HttpPost("Update/{imageId}")]
        public async Task<IActionResult> Update(Guid imageId, [FromBody] UpdateRequestModel request)
        {
            if (request.ImageName == null)
                return ControllerResult.FailureWithResourceMesg("NameNotSet", this);
            if (request.Description == null)
                return ControllerResult.FailureWithResourceMesg("DescriptionNotSet", this);
            if (request.Source == null)
                return ControllerResult.FailureWithResourceMesg("SourceNotSet", this);
            if (request.VersionDescription == null)
                return ControllerResult.FailureWithResourceMesg("VersionDescriptionNotSet", this);

            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                var applicationRequest = new UpdateImageMetadata.Request(imageId, user, request.ImageName, request.Source, request.Description, request.VersionDescription);
                await new UpdateImageMetadata(dbContext, localizer).RunAsync(applicationRequest);
                var toastText = $"{localizer["SuccessfullyUpdatedImage"]} '{request.ImageName}'";
                return ControllerResult.Success(toastText, this);
            }
            catch (Exception e)
            {
                return ControllerResult.Failure(e, this);
            }
        }
        public sealed class UpdateRequestModel
        {
            public string? ImageName { get; set; } = null;
            public string? Source { get; set; } = null;
            public string? Description { get; set; } = null;
            public string? VersionDescription { get; set; } = null;
        }
        #endregion
        #region GetStaticText
        [HttpGet("GetStaticText")]
        public IActionResult GetStaticText()
        {
            return Ok(new { copyToClipboardToastTitleOnSuccess = localizer["CopyToClipboardToastTitleOnSuccess"].Value, copyToClipboardToastTitleOnFailure = localizer["CopyToClipboardToastTitleOnFailure"].Value });
        }
        #endregion
        #region Delete
        [HttpPost("Delete/{imageId}")]
        public async Task<IActionResult> Delete(Guid imageId, [FromBody] DeleteRequest request)
        {
            try
            {
                if (request.DeletionDescription == null)
                    return ControllerResult.FailureWithResourceMesg("DeletionDescriptionNotSet", this);

                var user = await userManager.GetUserAsync(HttpContext.User);
                var applicationRequest = new DeleteImage.Request(user, imageId, request.DeletionDescription);
                var imageName = await new DeleteImage(dbContext, localizer).RunAsync(applicationRequest);
                var toastText = $"{localizer["SuccessfullyDeletedImage"]} '{imageName}'";
                return Ok(new { ToastText = toastText, ToastTitle = localizer["Success"] });
            }
            catch (Exception e)
            {
                return ControllerError.BadRequest(e, this);
            }
        }
        public sealed class DeleteRequest
        {
            public string? DeletionDescription { get; set; } = null;
        }
        #endregion
        #region GetImageInfoForDeletion
        [HttpGet("GetImageInfoForDeletion/{imageId}")]
        public async Task<IActionResult> GetImageInfoForDeletion(Guid imageId)
        {
            try
            {
                GetImageInfo runner = new GetImageInfo(dbContext, localizer);
                var result = await runner.RunAsync(imageId);
                return Ok(new GetImageInfoForDeletionResult(result, localizer));
            }
            catch
            {
                return BadRequest();
            }
        }
        public sealed class GetImageInfoForDeletionResult
        {
            public GetImageInfoForDeletionResult(GetImageInfo.Result appResult, IStringLocalizer localizer)
            {
                ImageName = appResult.Name;
                CardCount = appResult.CardCount;
                CurrentVersionUserName = appResult.Owner.UserName;
                Description = appResult.Description;
                Source = appResult.Source;
                InitialUploadUtcDate = appResult.InitialUploadUtcDate;
                DeletionAlertMessage = localizer["AreYouSure"].Value;
                LastChangeUtcDate = appResult.LastChangeUtcDate;
                CurrentVersionDescription = appResult.CurrentVersionDescription;
            }
            public string ImageName { get; }
            public int CardCount { get; }
            public string CurrentVersionUserName { get; }
            public string Description { get; }
            public string Source { get; }
            public DateTime InitialUploadUtcDate { get; }
            public string DeletionAlertMessage { get; }
            public DateTime LastChangeUtcDate { get; }
            public string CurrentVersionDescription { get; }
        }
        #endregion
        #region ImageVersions
        [HttpGet("ImageVersions/{imageId}")]
        public async Task<IActionResult> ImageVersions(Guid imageId)
        {
            try
            {
                var appResults = await new GetImageVersions(dbContext, localizer).RunAsync(imageId);
                var result = appResults.Select(appResult => new ImageVersion(appResult, localizer));
                return Ok(result);
            }
            catch (Exception e)
            {
                return ControllerError.BadRequest(e, this);
            }
        }
        public sealed class ImageVersion
        {
            public ImageVersion(GetImageVersions.ResultImageVersion appResult, IStringLocalizer<MediaController> localizer)
            {
                VersionUtcDate = appResult.VersionUtcDate;
                Author = appResult.Author;
                VersionDescription = appResult.VersionDescription;
                var fieldsDisplayNames = appResult.ChangedFieldNames.Select(fieldName => localizer[fieldName].Value);
                ChangedFieldList = string.Join(',', fieldsDisplayNames);
            }
            public DateTime VersionUtcDate { get; }
            public string Author { get; }
            public string VersionDescription { get; }
            public string ChangedFieldList { get; }
        }
        #endregion
        #region GetBigSizeImageLabels
        [HttpGet("GetBigSizeImageLabels")]
        public IActionResult GetBigSizeImageLabels()
        {
            return Ok(new
            {
                //Labels for fields
                name = localizer["BigSizeImageLabel_Name"].Value,
                description = localizer["BigSizeImageLabel_Description"].Value,
                source = localizer["BigSizeImageLabel_Source"].Value,
                initialVersionCreatedOn = localizer["BigSizeImageLabel_InitialVersionCreatedOn"].Value,
                initialVersionCreatedBy = localizer["BigSizeImageLabel_InitialVersionCreatedBy"].Value,
                currentVersionCreatedOn = localizer["BigSizeImageLabel_CurrentVersionCreatedOn"].Value,
                currentVersionCreatedBy = localizer["BigSizeImageLabel_CurrentVersionCreatedBy"].Value,
                currentVersionDescription = localizer["BigSizeImageLabel_CurrentVersionDescription"].Value,
                numberOfCards = localizer["BigSizeImageLabel_NumberOfCards"].Value,
                originalImageContentType = localizer["BigSizeImageLabel_originalImageContentType"].Value,
                originalImageSize = localizer["BigSizeImageLabel_OriginalImageSize"].Value,
                smallSize = localizer["BigSizeImageLabel_SmallSize"].Value,
                mediumSize = localizer["BigSizeImageLabel_MediumSize"].Value,
                bigSize = localizer["BigSizeImageLabel_BigSize"].Value,

                //Labels for Buttons
                removeButtonTitle = localizer["BigSizeImageLabel_Remove"].Value,
                copyToClipboardButtonTitle = localizer["BigSizeImageLabel_CopyToClipboard"].Value,
                closeButtonTitle = localizer["BigSizeImageLabel_CloseButtonTitle"].Value,
                editButtonTitle = localizer["BigSizeImageLabel_EditButtonTitle"].Value,
                versionHistoryButtonTitle = localizer["BigSizeImageLabel_VersionHistoryButtonTitle"].Value,

                //Labels for Messages
                copiedToClipboardToastTitleOnSuccess = localizer["BigSizeImageLabel_CopiedToClipboardToastTitleOnSuccess"].Value,
                copiedToClipboardToastTitleOnFailure = localizer["BigSizeImageLabel_CopiedToClipboardToastTitleOnFailure"].Value,
                downloadBiggestSize = localizer["BigSizeImageLabel_DownloadBiggestSize"].Value,
            });
        }
        #endregion
    }
}
