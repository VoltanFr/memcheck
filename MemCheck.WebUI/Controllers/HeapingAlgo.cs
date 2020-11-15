using MemCheck.Application;
using MemCheck.Application.Heaping;
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
    internal static class HeapingAlgo
    {
        public static string NameFromId(int id, IStringLocalizer<DecksController> localizer)
        {
            return localizer["HeapingAlgoNameForId" + id].Value;
        }
        public static string DescriptionFromId(int id, IStringLocalizer<DecksController> localizer)
        {
            return localizer["HeapingAlgoDescriptionForId" + id].Value;
        }
    }
}
