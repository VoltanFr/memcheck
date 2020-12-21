using Microsoft.Extensions.Localization;

namespace MemCheck.WebUI.Controllers
{
    public interface ILocalized
    {
        public IStringLocalizer Localizer { get; }
    }

}
