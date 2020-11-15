using MemCheck.Application;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.WebUI.Controllers
{
    public static class DisplayServices
    {
        public static string DateAsText(DateTime dt)
        {
            return dt.ToLocalTime().ToString("d");    //With time for developping and debugging. When we are ok with expiration algorithms, display only the date
        }
        public static string HeapName(int heap, IStringLocalizer localizer)
        {
            return heap == 0 ? localizer["UnknownCardsHeap"].Value : heap.ToString();
        }
        public static bool ShowDebugInfo(MemCheckUser? user)
        {
            return user != null && (user.UserName == "Voltan" || user.UserName == "Toto1");
        }
    }
}
