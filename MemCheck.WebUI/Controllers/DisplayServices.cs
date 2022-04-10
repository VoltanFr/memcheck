using MemCheck.Application.QueryValidation;
using MemCheck.Domain;
using System.Globalization;

namespace MemCheck.WebUI.Controllers
{
    public static class DisplayServices
    {
        public static string HeapName(int heap, ILocalized localizer)
        {
            return heap == 0 ? localizer.GetLocalized("UnknownCardsHeap") : heap.ToString(CultureInfo.InvariantCulture);
        }
        public static bool ShowDebugInfo(MemCheckUser? user)
        {
            return user != null && (user.UserName == "Voltan" || user.UserName == "Toto1");
        }
    }
}
