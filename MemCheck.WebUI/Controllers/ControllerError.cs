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
    public sealed class ControllerError
    {
        #region private methods
        private ControllerError(string text, bool isControllerBug, IStringLocalizer localizer)
        {
            Title = localizer["Failure"].Value;
            Text = text;
            ShowStatus = isControllerBug;
        }
        private static string GetText(Exception e)
        {
            if (e is RequestInputException)
                return e.Message;

            var text = $"Exception class {e.GetType().Name}, message: '{e.Message}'";
            if (e.InnerException != null)
                text += $"\r\nInner exception class {e.InnerException.GetType().Name}, message: '{e.InnerException.Message}'";
            return text;
        }
        #endregion
        public string Title { get; }
        public string Text { get; }
        public bool ShowStatus { get; }
        public static ActionResult BadRequest<TController>(string text, TController controller) where TController : Controller, ILocalized
        {
            return controller.BadRequest(new ControllerError(text, false, controller.Localizer));
        }
        public static ActionResult BadRequest<TController>(Exception e, TController controller) where TController : Controller, ILocalized
        {
            return controller.BadRequest(new ControllerError(GetText(e), !(e is RequestInputException), controller.Localizer));
        }
    }
}
