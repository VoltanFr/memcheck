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
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.WebUI.Controllers
{
    [Route("[controller]"), Authorize]
    public class MediaController : MemCheckController
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        private readonly UserManager<MemCheckUser> userManager;
        #endregion
        public MediaController(MemCheckDbContext dbContext, UserManager<MemCheckUser> userManager, IStringLocalizer<MediaController> localizer) : base(localizer)
        {
            this.dbContext = dbContext;
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

            var user = await userManager.GetUserAsync(HttpContext.User);
            if (user == null)
                return ControllerResultWithToast.FailureWithResourceMesg("NeedLogin", this);

            using (var stream = request.File.OpenReadStream())
            using (var reader = new BinaryReader(stream))
            {
                var fileContent = reader.ReadBytes((int)request.File.Length);
                var applicationRequest = new StoreImage.Request(user, request.Name, request.Description, request.Source, request.File.ContentType, fileContent);
                var id = await new StoreImage(dbContext, Localizer).RunAsync(applicationRequest);
                if (id == Guid.Empty)
                    throw new ApplicationException("Stored image with empty GUID as id");
                return ControllerResultWithToast.Success(Localize("ImageSavedWithName") + $" '{applicationRequest.Name}'", this);
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
            var result = new GetImageList(dbContext).Run(request.PageSize, request.PageNo, request.Filter ?? "");
            return Ok(new GetImageListViewModel(result, Localizer));
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
            var appRequest = new GetImageInfo(dbContext, Localizer);
            var result = await appRequest.RunAsync(imageId);
            return Ok(new GetImageMetadataViewModel(result.Name, result.Source, result.Description));
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
                return ControllerResultWithToast.FailureWithResourceMesg("NameNotSet", this);
            if (request.Description == null)
                return ControllerResultWithToast.FailureWithResourceMesg("DescriptionNotSet", this);
            if (request.Source == null)
                return ControllerResultWithToast.FailureWithResourceMesg("SourceNotSet", this);
            if (request.VersionDescription == null)
                return ControllerResultWithToast.FailureWithResourceMesg("VersionDescriptionNotSet", this);

            var user = await userManager.GetUserAsync(HttpContext.User);
            var applicationRequest = new UpdateImageMetadata.Request(imageId, user, request.ImageName, request.Source, request.Description, request.VersionDescription);
            await new UpdateImageMetadata(dbContext, Localizer).RunAsync(applicationRequest);
            var toastText = $"{Localize("SuccessfullyUpdatedImage")} '{request.ImageName}'";
            return ControllerResultWithToast.Success(toastText, this);
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
            return Ok(new { copyToClipboardToastTitleOnSuccess = Localize("CopyToClipboardToastTitleOnSuccess"), copyToClipboardToastTitleOnFailure = Localize("CopyToClipboardToastTitleOnFailure") });
        }
        #endregion
        #region Delete
        [HttpPost("Delete/{imageId}")]
        public async Task<IActionResult> Delete(Guid imageId, [FromBody] DeleteRequest request)
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var applicationRequest = new DeleteImage.Request(user, imageId, request.DeletionDescription);
            var imageName = await new DeleteImage(dbContext, Localizer).RunAsync(applicationRequest);
            var toastText = $"{Localize("SuccessfullyDeletedImage")} '{imageName}'";
            return Ok(new { ToastText = toastText, ToastTitle = Localize("Success") });
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
            try
            {
                GetImageInfo runner = new GetImageInfo(dbContext, Localizer);
                var result = await runner.RunAsync(imageId);
                return Ok(new GetImageInfoForDeletionResult(result, Localizer));
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
            var appResults = await new GetImageVersions(dbContext, Localizer).RunAsync(imageId);
            var result = appResults.Select(appResult => new ImageVersion(appResult, Localizer));
            return Ok(result);
        }
        public sealed class ImageVersion
        {
            public ImageVersion(GetImageVersions.ResultImageVersion appResult, IStringLocalizer localizer)
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
                name = Localize("BigSizeImageLabel_Name"),
                description = Localize("BigSizeImageLabel_Description"),
                source = Localize("BigSizeImageLabel_Source"),
                initialVersionCreatedOn = Localize("BigSizeImageLabel_InitialVersionCreatedOn"),
                initialVersionCreatedBy = Localize("BigSizeImageLabel_InitialVersionCreatedBy"),
                currentVersionCreatedOn = Localize("BigSizeImageLabel_CurrentVersionCreatedOn"),
                currentVersionDescription = Localize("BigSizeImageLabel_CurrentVersionDescription"),
                numberOfCards = Localize("BigSizeImageLabel_NumberOfCards"),
                originalImageContentType = Localize("BigSizeImageLabel_originalImageContentType"),
                originalImageSize = Localize("BigSizeImageLabel_OriginalImageSize"),
                smallSize = Localize("BigSizeImageLabel_SmallSize"),
                mediumSize = Localize("BigSizeImageLabel_MediumSize"),
                bigSize = Localize("BigSizeImageLabel_BigSize"),

                //Labels for Buttons
                removeButtonTitle = Localize("BigSizeImageLabel_Remove"),
                copyToClipboardButtonTitle = Localize("BigSizeImageLabel_CopyToClipboard"),
                closeButtonTitle = Localize("BigSizeImageLabel_CloseButtonTitle"),
                editButtonTitle = Localize("BigSizeImageLabel_EditButtonTitle"),
                versionHistoryButtonTitle = Localize("BigSizeImageLabel_VersionHistoryButtonTitle"),

                //Labels for Messages
                copiedToClipboardToastTitleOnSuccess = Localize("BigSizeImageLabel_CopiedToClipboardToastTitleOnSuccess"),
                copiedToClipboardToastTitleOnFailure = Localize("BigSizeImageLabel_CopiedToClipboardToastTitleOnFailure"),
                downloadBiggestSize = Localize("BigSizeImageLabel_DownloadBiggestSize"),
            });
        }
        #endregion
    }
}
