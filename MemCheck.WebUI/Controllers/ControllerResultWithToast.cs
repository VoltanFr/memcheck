using MemCheck.Application.QueryValidation;
using Microsoft.AspNetCore.Mvc;
using System;

namespace MemCheck.WebUI.Controllers
{
    public sealed class ControllerResultWithToast
    {
        #region Private methods
        private ControllerResultWithToast(string toastTitle, string toastText, bool showStatus)
        {
            ToastTitle = toastTitle;
            ToastText = toastText;
            ShowStatus = showStatus;
        }
        private static string Localize<TController>(string resourceName, TController controller) where TController : Controller, ILocalized
        {
            var result = controller.Localizer[resourceName];
            if (result.ResourceNotFound)
                throw new InvalidOperationException($"Ressource '{resourceName}' not found in localizer '{controller.Localizer}'");
            return result.Value;
        }
        private static string TextFromException(Exception e)
        {
            if (e is RequestInputException)
                return e.Message;

            var text = $"Exception class {e.GetType().Name}, message: '{e.Message}'";
            if (e.InnerException != null)
                text += $"\r\nInner exception class {e.InnerException.GetType().Name}, message: '{e.InnerException.Message}'";
            return text;
        }
        private static bool IsBug(Exception e)
        {
            return !(e is RequestInputException);
        }
        #endregion
        public static BadRequestObjectResult Failure<TController>(string toastText, bool showStatus, TController controller) where TController : Controller, ILocalized
        {
            return controller.BadRequest(new ControllerResultWithToast(Localize("Failure", controller), toastText, showStatus));
        }
        public static BadRequestObjectResult FailureWithResourceMesg<TController>(string textResourceName, TController controller) where TController : Controller, ILocalized
        {
            return controller.BadRequest(new ControllerResultWithToast(Localize("Failure", controller), Localize(textResourceName, controller), false));
        }
        public static OkObjectResult Success<TController>(string toastText, TController controller) where TController : Controller, ILocalized
        {
            return controller.Ok(new ControllerResultWithToast(Localize("Success", controller), toastText, false));
        }
        public string ToastTitle { get; }
        public string ToastText { get; }
        public bool ShowStatus { get; }

    }

}
