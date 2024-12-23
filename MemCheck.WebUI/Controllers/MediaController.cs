﻿using MemCheck.Application;
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

namespace MemCheck.WebUI.Controllers;

// Some of the pages in this controller have specific authorizations, see Startup.ConfigureServices

[Route("[controller]")]
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
    [HttpPost("UploadImage"), Authorize]
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
        var appResult = await new StoreImage(callContext).RunAsync(applicationRequest);
        var toastTitle = GetLocalized("Success");
        var toastMesg = $"{GetLocalized("ImageSavedWithName")} '{applicationRequest.Name.Trim()}'";
        var result = new UploadImageResult(toastTitle, toastMesg, appResult.ImageId);
        return Ok(result);
    }
    public sealed class UploadImageRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Source { get; set; }
        public IFormFile? File { get; set; }
    }
    public sealed record UploadImageResult(string ToastTitle, string ToastText, Guid ImageId);
    #endregion
    #region GetImageList
    [HttpPost("GetImageList"), Authorize]
    public async Task<IActionResult> GetImageListAsync([FromBody] GetImageListRequest request)
    {
        CheckBodyParameter(request);
        var result = await new GetImageList(callContext).RunAsync(new GetImageList.Request(request.PageSize, request.PageNo, request.Filter == null ? "" : request.Filter.Trim()));
        return Ok(new GetImageListViewModel(result));
    }
    public sealed class GetImageListRequest
    {
        public int PageSize { get; set; }
        public int PageNo { get; set; }
        public string? Filter { get; set; }
    }
    public sealed class GetImageListViewModel
    {
        public GetImageListViewModel(GetImageList.Result applicationResult)
        {
            TotalCount = applicationResult.TotalCount;
            PageCount = applicationResult.PageCount;
            Images = applicationResult.Images.Select(img => new GetImageListImageViewModel(img));
        }
        public int TotalCount { get; }
        public int PageCount { get; }
        public IEnumerable<GetImageListImageViewModel> Images { get; }
    }
    public sealed class GetImageListImageViewModel
    {
        public GetImageListImageViewModel(GetImageList.ResultImage img)
        {
            ImageId = img.ImageId;
            ImageName = img.ImageName;
            CardCount = img.CardCount;
        }
        public Guid ImageId { get; }
        public string ImageName { get; }
        public int CardCount { get; }
    }
    #endregion
    #region GetImageMetadataForEdit
    [HttpGet("GetImageMetadataForEdit/{imageId}")]
    public async Task<IActionResult> GetImageMetadataForEdit(Guid imageId)
    {
        var appRequest = new GetImageInfoFromId(callContext);
        var result = await appRequest.RunAsync(new GetImageInfoFromId.Request(imageId));
        return Ok(new GetImageMetadataForEditViewModel(result.ImageName, result.Source, result.Description));
    }
    public sealed class GetImageMetadataForEditViewModel
    {
        public GetImageMetadataForEditViewModel(string imageName, string source, string description)
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
    #region GetImageMetadataFromName
    [HttpPost("GetImageMetadataFromName")] // No authorization: when running a demo without user, this is needed
    public async Task<IActionResult> GetImageMetadataFromName([FromBody] GetImageMetadataFromNameRequest request)
    {
        try
        {
            var appRequest = new GetImageInfoFromName(callContext);
            var result = await appRequest.RunAsync(new GetImageInfoFromName.Request(request.ImageName));
            return Ok(new GetImageMetadataFromNameViewModel(result.Id, result.Description, result.Source, result.InitialUploadUtcDate, result.InitialVersionCreator, result.CurrentVersionUtcDate, result.CurrentVersionDescription, result.CardCount, result.OriginalImageContentType, result.OriginalImageSize, result.SmallSize, result.MediumSize, result.BigSize));
        }
        catch (ImageNotFoundException)
        {
            return NotFound();
        }
    }
    public sealed class GetImageMetadataFromNameRequest
    {
        public string ImageName { get; set; } = null!;
    }
    public sealed class GetImageMetadataFromNameViewModel
    {
        public GetImageMetadataFromNameViewModel(Guid imageId, string description, string source, DateTime initialUploadUtcDate, string initialVersionCreator, DateTime currentVersionUtcDate, string currentVersionDescription, int cardCount, string originalImageContentType, int originalImageSize, int smallSize, int mediumSize, int bigSize)
        {
            ImageId = imageId;
            Description = description;
            Source = source;
            InitialUploadUtcDate = initialUploadUtcDate;
            InitialVersionCreator = initialVersionCreator;
            CurrentVersionUtcDate = currentVersionUtcDate;
            CurrentVersionDescription = currentVersionDescription;
            CardCount = cardCount;
            OriginalImageContentType = originalImageContentType;
            OriginalImageSize = originalImageSize;
            SmallSize = smallSize;
            MediumSize = mediumSize;
            BigSize = bigSize;
        }
        public Guid ImageId { get; }
        public string Description { get; }
        public string Source { get; }
        public DateTime InitialUploadUtcDate { get; }
        public string InitialVersionCreator { get; }
        public DateTime CurrentVersionUtcDate { get; }
        public string CurrentVersionDescription { get; }
        public int CardCount { get; }
        public string OriginalImageContentType { get; }
        public int OriginalImageSize { get; }
        public int SmallSize { get; }
        public int MediumSize { get; }
        public int BigSize { get; }
    }
    #endregion
    #region GetImageMetadataFromId
    [HttpGet("GetImageMetadataFromId/{imageId}"), Authorize]
    public async Task<IActionResult> GetImageMetadataFromId(Guid imageId)
    {
        try
        {
            var appRequest = new GetImageInfoFromId(callContext);
            var result = await appRequest.RunAsync(new GetImageInfoFromId.Request(imageId));
            return Ok(new GetImageMetadataFromIdViewModel(imageId, result.ImageName, result.Description, result.Source, result.InitialUploadUtcDate, result.CurrentVersionCreatorName, result.LastChangeUtcDate, result.CurrentVersionDescription, result.CardCount, result.OriginalImageContentType, result.OriginalImageSize, result.SmallSize, result.MediumSize, result.BigSize));
        }
        catch (ImageNotFoundException)
        {
            return NotFound();
        }
    }
    public sealed class GetImageMetadataFromIdRequest
    {
        public string ImageName { get; set; } = null!;
    }
    public sealed class GetImageMetadataFromIdViewModel
    {
        public GetImageMetadataFromIdViewModel(Guid imageId, string imageName, string description, string source, DateTime initialUploadUtcDate, string currentVersionCreatorName, DateTime currentVersionUtcDate, string currentVersionDescription, int cardCount, string originalImageContentType, int originalImageSize, int smallSize, int mediumSize, int bigSize)
        {
            ImageId = imageId;
            ImageName = imageName;
            Description = description;
            Source = source;
            InitialUploadUtcDate = initialUploadUtcDate;
            CurrentVersionCreatorName = currentVersionCreatorName;
            CurrentVersionUtcDate = currentVersionUtcDate;
            CurrentVersionDescription = currentVersionDescription;
            CardCount = cardCount;
            OriginalImageContentType = originalImageContentType;
            OriginalImageSize = originalImageSize;
            SmallSize = smallSize;
            MediumSize = mediumSize;
            BigSize = bigSize;
        }
        public Guid ImageId { get; }
        public string ImageName { get; }
        public string Description { get; }
        public string Source { get; }
        public DateTime InitialUploadUtcDate { get; }
        public string CurrentVersionCreatorName { get; }
        public DateTime CurrentVersionUtcDate { get; }
        public string CurrentVersionDescription { get; }
        public int CardCount { get; }
        public string OriginalImageContentType { get; }
        public int OriginalImageSize { get; }
        public int SmallSize { get; }
        public int MediumSize { get; }
        public int BigSize { get; }
    }
    #endregion
    #region Update
    [HttpPost("Update/{imageId}"), Authorize]
    public async Task<IActionResult> Update(Guid imageId, [FromBody] UpdateRequestModel request)
    {
        CheckBodyParameter(request);
        var userId = await UserServices.UserIdFromContextAsync(HttpContext, userManager);
        var applicationRequest = new UpdateImageMetadata.Request(imageId, userId, request.ImageName.Trim(), request.Source.Trim(), request.Description.Trim(), request.VersionDescription.Trim());
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
    #region Delete
    [HttpPost("Delete/{imageId}"), Authorize]
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
    [HttpGet("GetImageInfoForDeletion/{imageId}"), Authorize]
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
            ImageName = appResult.ImageName;
            CardCount = appResult.CardCount;
            CurrentVersionCreatorName = appResult.CurrentVersionCreatorName;
            Description = appResult.Description;
            Source = appResult.Source;
            InitialUploadUtcDate = appResult.InitialUploadUtcDate;
            DeletionAlertMessage = localizer.GetLocalized("AreYouSure");
            LastChangeUtcDate = appResult.LastChangeUtcDate;
            CurrentVersionDescription = appResult.CurrentVersionDescription;
        }
        public string ImageName { get; }
        public int CardCount { get; }
        public string CurrentVersionCreatorName { get; }
        public string Description { get; }
        public string Source { get; }
        public DateTime InitialUploadUtcDate { get; }
        public string DeletionAlertMessage { get; }
        public DateTime LastChangeUtcDate { get; }
        public string CurrentVersionDescription { get; }
    }
    #endregion
    #region ImageVersions
    [HttpGet("ImageVersions/{imageId}"), Authorize]
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
}
