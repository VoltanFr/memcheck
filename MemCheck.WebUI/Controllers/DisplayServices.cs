using MemCheck.Application.QueryValidation;
using MemCheck.Domain;
using System;

namespace MemCheck.WebUI.Controllers
{
    public static class DisplayServices
    {
        public static string DateAsText(DateTime dt)
        {
            return dt.ToLocalTime().ToString("d");    //With time for developping and debugging. When we are ok with expiration algorithms, display only the date
        }
        public static string HeapName(int heap, ILocalized localizer)
        {
            return heap == 0 ? localizer.Get("UnknownCardsHeap") : heap.ToString();
        }
        public static bool ShowDebugInfo(MemCheckUser? user)
        {
            return user != null && (user.UserName == "Voltan" || user.UserName == "Toto1");
        }
    }
}
