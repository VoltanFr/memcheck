using System;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MemCheck.WebUI.Pages.Admin
{
    public static class ManageNavPages
    {
        public static string Index => "Index";
        public static string Users => "Users";
        public static string Languages => "Languages";
        public static string Notifier => "Notifier";

        public static string PageNavClass(ViewContext viewContext, string page)
        {
            var activePage = viewContext.ViewData["ActivePage"] as string
                ?? System.IO.Path.GetFileNameWithoutExtension(viewContext.ActionDescriptor.DisplayName);
            return string.Equals(activePage, page, StringComparison.OrdinalIgnoreCase) ? "active" : "";
        }
    }
}
