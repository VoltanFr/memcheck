using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MemCheck.WebUI.Pages.Admin
{
    public static class ManageNavPages
    {
        public static string Index => "Index";

        public static string IndexNavClass(ViewContext viewContext) => PageNavClass(viewContext, Index);

        private static string PageNavClass(ViewContext viewContext, string page)
        {
            var activePage = viewContext.ViewData["ActivePage"] as string
                ?? System.IO.Path.GetFileNameWithoutExtension(viewContext.ActionDescriptor.DisplayName);
            return string.Equals(activePage, page, StringComparison.OrdinalIgnoreCase) ? "active" : "";
        }
    }
}
