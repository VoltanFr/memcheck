using MemCheck.Application.QueryValidation;
using Microsoft.AspNetCore.Mvc;

namespace MemCheck.WebUI.Controllers;

public sealed class ControllerResultWithToast
{
    #region Private methods
    private ControllerResultWithToast(string toastTitle, string toastText, bool showStatus)
    {
        ToastTitle = toastTitle;
        ToastText = toastText;
        ShowStatus = showStatus;
    }
    #endregion
    public static BadRequestObjectResult Failure<TController>(string toastText, bool showStatus, TController controller) where TController : Controller, ILocalized
    {
        return controller.BadRequest(new ControllerResultWithToast(controller.GetLocalized("Failure"), toastText, showStatus));
    }
    public static BadRequestObjectResult FailureWithResourceMesg<TController>(string textResourceName, TController controller) where TController : Controller, ILocalized
    {
        return controller.BadRequest(new ControllerResultWithToast(controller.GetLocalized("Failure"), controller.GetLocalized(textResourceName), false));
    }
    public static OkObjectResult Success<TController>(string toastText, TController controller) where TController : Controller, ILocalized
    {
        return controller.Ok(new ControllerResultWithToast(controller.GetLocalized("Success"), toastText, false));
    }
    public string ToastTitle { get; }
    public string ToastText { get; }
    public bool ShowStatus { get; }

}
