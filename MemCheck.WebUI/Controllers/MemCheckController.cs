using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System;
using System.Runtime.CompilerServices;

namespace MemCheck.WebUI.Controllers
{
    public abstract class MemCheckController : Controller, ILocalized
    {
        #region Fields
        private readonly IStringLocalizer localizer;
        #endregion
        protected MemCheckController(IStringLocalizer localizer)
        {
            this.localizer = localizer;
        }
        protected void CheckBodyParameter(object? bodyParameter, [CallerMemberName] string memberName = "")
        {
            //Getting a null is generally a bug related to failure to marshall parameters from Javascript to asp net core, so should not happen IRL, but checking helps when coding
            if (bodyParameter == null)
                throw new InvalidProgramException($"Received null body parameter in {GetType().Name}.{memberName}");
            var properties = bodyParameter.GetType().GetProperties();
            foreach (var property in properties)
                if (property.GetValue(bodyParameter) == null)
                    throw new InvalidProgramException($"Property '{property.Name}' is null in body parameter of {GetType().Name}.{memberName}");
        }
        protected string Localize(string resourceName)
        {
            var result = localizer[resourceName];
            if (result.ResourceNotFound)
                throw new InvalidOperationException($"Ressource '{resourceName}' not found in localizer of {GetType().Name}");
            return result.Value;
        }
        public IStringLocalizer Localizer => localizer;
    }
}
