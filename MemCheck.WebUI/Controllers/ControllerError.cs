using MemCheck.Application;
using MemCheck.Application.QueryValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System;

namespace MemCheck.WebUI.Controllers
{
    public interface ILocalized
    {
        public IStringLocalizer Localizer { get; }
    }

    public sealed class ControllerResult
    {
        #region Private methods
        private ControllerResult(string toastTitle, string toastText, bool showStatus)
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
            return controller.BadRequest(new ControllerResult(Localize("Failure", controller), toastText, showStatus));
        }
        public static BadRequestObjectResult FailureWithResourceMesg<TController>(string textResourceName, TController controller) where TController : Controller, ILocalized
        {
            return controller.BadRequest(new ControllerResult(Localize("Failure", controller), Localize(textResourceName, controller), false));
        }
        public static OkObjectResult Success<TController>(string toastText, TController controller) where TController : Controller, ILocalized
        {
            return controller.Ok(new ControllerResult(Localize("Success", controller), toastText, false));
        }
        public string ToastTitle { get; }
        public string ToastText { get; }
        public bool ShowStatus { get; }

    }

}
