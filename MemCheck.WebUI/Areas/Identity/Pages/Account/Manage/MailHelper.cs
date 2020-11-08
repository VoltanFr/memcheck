using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using MemCheck.Domain;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MemCheck.WebUI.Areas.Identity.Pages.Account.Manage
{
    public static class MailHelper
    {
        public static string GetBody(MemCheckUser user, string callbackUrl, string verb)
        {
            return $"Hello {user.UserName}. {verb} by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.";
        }
    }
}
