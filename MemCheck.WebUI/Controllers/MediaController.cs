using MemCheck.Application;
using MemCheck.Application.Images;
using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.WebUI.Controllers
{
    [Route("[controller]"), Authorize]
    public class MediaController : MemCheckController
    {
        #region Fields
        private readonly CallContext callContext;
        private readonly UserManager<MemCheckUser> userManager;
        #endregion
        public MediaController(MemCheckDbContext dbContext, UserManager<MemCheckUser> userManager, IStringLocalizer<MediaController> localizer, TelemetryClient telemetryClient) : base(localizer)
        {
            callContext = new CallContext(dbContext, new MemCheckTelemetryClient(telemetryClient), this, new ProdRoleChecker(userManager));
            this.userManager = userManager;
        }
        #region UploadImage
        [HttpPost("UploadImage")]
        public async Task<IActionResult> UploadImage([FromForm] UploadImageRequest request)
        {
            if (request.Name == null)
                return ControllerResultWithToast.FailureWithResourceMesg("NameNotSet", this);
            if (request.Description == null)
                return ControllerResultWithToast.FailureWithResourceMesg("DescriptionNotSet", this);
            if (request.Source == null)
                return ControllerResultWithToast.FailureWithResourceMesg("SourceNotSet", this);
            if (request.File == null)
                return ControllerResultWithToast.FailureWithResourceMesg("FileNotSet", this);

            var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);

            using var stream = request.File.OpenReadStream();
            using var reader = new BinaryReader(stream);
            var fileContent = reader.ReadBytes((int)request.File.Length);
            var applicationRequest = new StoreImage.Request(userId, request.Name.Trim(), request.Description.Trim(), request.Source.Trim(), request.File.ContentType, fileContent);
            await new StoreImage(callContext).RunAsync(applicationRequest);
            return ControllerResultWithToast.Success($"{GetLocalized("ImageSavedWithName")} '{applicationRequest.Name.Trim()}'", this);
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
        public async Task<IActionResult> GetImageListAsync([FromBody] GetImageListRequest request)
        {
            CheckBodyParameter(request);
            var result = await new GetImageList(callContext).RunAsync(new GetImageList.Request(request.PageSize, request.PageNo, request.Filter == null ? "" : request.Filter.Trim()));
            return Ok(new GetImageListViewModel(result, this));
        }
        public sealed class GetImageListRequest
        {
            public int PageSize { get; set; }
            public int PageNo { get; set; }
            public string? Filter { get; set; }
        }
        public sealed class GetImageListViewModel
        {
            public GetImageListViewModel(GetImageList.Result applicationResult, ILocalized localizer)
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
            public GetImageListImageViewModel(GetImageList.ResultImage img, ILocalized localizer)
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
                RemoveAlertMessage = $"{localizer.GetLocalized("SureYouWantToDeletePart1")} '{ImageName}' ? {localizer.GetLocalized("SureYouWantToDeletePart2")} {UploaderUserName} {localizer.GetLocalized("SureYouWantToDeletePart3")} ";
                CurrentVersionDescription = img.CurrentVersionDescription;
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
            public string CurrentVersionDescription { get; }
        }
        #endregion
        #region GetImageMetadata
        [HttpGet("GetImageMetadata/{imageId}")]
        public async Task<IActionResult> GetImageMetadata(Guid imageId)
        {
            var appRequest = new GetImageInfoFromId(callContext);
            var result = await appRequest.RunAsync(new GetImageInfoFromId.Request(imageId));
            return Ok(new GetImageMetadataViewModel(result.Name, result.Source, result.Description));
        }
        public sealed class GetImageMetadataViewModel
        {
            public GetImageMetadataViewModel(string imageName, string source, string description)
            {
                ImageName = imageName;
                Source = source;
                Description = description;
            }
            public string ImageName { get; }
            public string Source { get; }
            public string Description { get; }
        }
        #endregion
        #region Update
        [HttpPost("Update/{imageId}")]
        public async Task<IActionResult> Update(Guid imageId, [FromBody] UpdateRequestModel request)
        {
            CheckBodyParameter(request);
            var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
            var applicationRequest = new UpdateImageMetadata.Request(imageId, userId, request.ImageName, request.Source, request.Description, request.VersionDescription);
            await new UpdateImageMetadata(callContext).RunAsync(applicationRequest);
            var toastText = $"{GetLocalized("SuccessfullyUpdatedImage")} '{request.ImageName}'";
            return ControllerResultWithToast.Success(toastText, this);
        }
        public sealed class UpdateRequestModel
        {
            public string ImageName { get; set; } = null!;
            public string Source { get; set; } = null!;
            public string Description { get; set; } = null!;
            public string VersionDescription { get; set; } = null!;
        }
        #endregion
        #region GetStaticText
        [HttpGet("GetStaticText")]
        public IActionResult GetStaticText()
        {
            return Ok(new { copyToClipboardToastTitleOnSuccess = GetLocalized("CopyToClipboardToastTitleOnSuccess"), copyToClipboardToastTitleOnFailure = GetLocalized("CopyToClipboardToastTitleOnFailure") });
        }
        #endregion
        #region Delete
        [HttpPost("Delete/{imageId}")]
        public async Task<IActionResult> Delete(Guid imageId, [FromBody] DeleteRequest request)
        {
            CheckBodyParameter(request);
            var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
            var applicationRequest = new DeleteImage.Request(userId, imageId, request.DeletionDescription);
            var imageName = await new DeleteImage(callContext).RunAsync(applicationRequest);
            var toastText = $"{GetLocalized("SuccessfullyDeletedImage")} '{imageName}'";
            return ControllerResultWithToast.Success(toastText, this);
        }
        public sealed class DeleteRequest
        {
            public string DeletionDescription { get; set; } = null!;
        }
        #endregion
        #region GetImageInfoForDeletion
        [HttpGet("GetImageInfoForDeletion/{imageId}")]
        public async Task<IActionResult> GetImageInfoForDeletion(Guid imageId)
        {
            var runner = new GetImageInfoFromId(callContext);
            var result = await runner.RunAsync(new GetImageInfoFromId.Request(imageId));
            return Ok(new GetImageInfoForDeletionResult(result, this));
        }
        public sealed class GetImageInfoForDeletionResult
        {
            public GetImageInfoForDeletionResult(GetImageInfoFromId.Result appResult, ILocalized localizer)
            {
                ImageName = appResult.Name;
                CardCount = appResult.CardCount;
                CurrentVersionUserName = appResult.Owner.UserName;
                Description = appResult.Description;
                Source = appResult.Source;
                InitialUploadUtcDate = appResult.InitialUploadUtcDate;
                DeletionAlertMessage = localizer.GetLocalized("AreYouSure");
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
            var appResults = await new GetImageVersions(callContext).RunAsync(new GetImageVersions.Request(imageId));
            var result = appResults.Select(appResult => new ImageVersion(appResult, this));
            return Ok(result);
        }
        public sealed class ImageVersion
        {
            public ImageVersion(GetImageVersions.ResultImageVersion appResult, ILocalized localizer)
            {
                VersionUtcDate = appResult.VersionUtcDate;
                Author = appResult.Author;
                VersionDescription = appResult.VersionDescription;
                var fieldsDisplayNames = appResult.ChangedFieldNames.Select(fieldName => localizer.GetLocalized(fieldName));
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
                name = GetLocalized("BigSizeImageLabel_Name"),
                description = GetLocalized("BigSizeImageLabel_Description"),
                source = GetLocalized("BigSizeImageLabel_Source"),
                initialVersionCreatedOn = GetLocalized("BigSizeImageLabel_InitialVersionCreatedOn"),
                initialVersionCreatedBy = GetLocalized("BigSizeImageLabel_InitialVersionCreatedBy"),
                currentVersionCreatedOn = GetLocalized("BigSizeImageLabel_CurrentVersionCreatedOn"),
                currentVersionDescription = GetLocalized("BigSizeImageLabel_CurrentVersionDescription"),
                numberOfCards = GetLocalized("BigSizeImageLabel_NumberOfCards"),
                originalImageContentType = GetLocalized("BigSizeImageLabel_originalImageContentType"),
                originalImageSize = GetLocalized("BigSizeImageLabel_OriginalImageSize"),
                smallSize = GetLocalized("BigSizeImageLabel_SmallSize"),
                mediumSize = GetLocalized("BigSizeImageLabel_MediumSize"),
                bigSize = GetLocalized("BigSizeImageLabel_BigSize"),

                //Labels for Buttons
                removeButtonTitle = GetLocalized("BigSizeImageLabel_Remove"),
                copyToClipboardButtonTitle = GetLocalized("BigSizeImageLabel_CopyToClipboard"),
                closeButtonTitle = GetLocalized("BigSizeImageLabel_CloseButtonTitle"),
                editButtonTitle = GetLocalized("BigSizeImageLabel_EditButtonTitle"),
                versionHistoryButtonTitle = GetLocalized("BigSizeImageLabel_VersionHistoryButtonTitle"),

                //Labels for Messages
                copiedToClipboardToastTitleOnSuccess = GetLocalized("BigSizeImageLabel_CopiedToClipboardToastTitleOnSuccess"),
                copiedToClipboardToastTitleOnFailure = GetLocalized("BigSizeImageLabel_CopiedToClipboardToastTitleOnFailure"),
                downloadBiggestSize = GetLocalized("BigSizeImageLabel_DownloadBiggestSize"),
            });
        }
        #endregion
    }
}
