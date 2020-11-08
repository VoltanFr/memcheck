using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Markdig;
using Markdig.Extensions.AutoLinks;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using MemCheck.Application;
using MemCheck.Application.Heaping;
using MemCheck.Database;
using MemCheck.WebUI.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Localization;

namespace MemCheck.WebUI.Pages.Doc
{
    public sealed class MdRendererModel : PageModel
    {
        private IWebHostEnvironment environment;
        public MdRendererModel(IWebHostEnvironment environment)
        {
            this.environment = environment;
        }
        [BindProperty(SupportsGet = true)] public string refererRoute { get; set; } = "";
        [BindProperty(SupportsGet = true)] public string cultureName { get; set; } = "";
        public string PageContents
        {
            get
            {
                var baseDir = Path.Combine(environment.ContentRootPath, "wwwroot", "mddoc");
                var splittedRefererRoute = refererRoute.Split('/');
                var refererPath = Path.Combine(splittedRefererRoute.SkipLast(1).ToArray());
                var refererPage = splittedRefererRoute.Last();
                var filePath = Path.Combine(baseDir, refererPath, $"{refererPage}-{cultureName}.md");
                if (!System.IO.File.Exists(filePath))
                {
                    filePath = Path.Combine(baseDir, $"index-{cultureName}.md");
                    if (!System.IO.File.Exists(filePath))
                        filePath = Path.Combine(baseDir, "index-en.md");
                }
                var markdown = System.IO.File.ReadAllText(filePath);



                using (var htmlWriter = new StringWriter())
                {

                    var pipeline = new MarkdownPipelineBuilder()
                               .UseSoftlineBreakAsHardlineBreak()
                               .UseAutoLinks(new AutoLinkOptions() { OpenInNewWindow = true });

                    var document = Markdown.Parse(markdown, pipeline.Build());

                    //foreach (var descendant in document.Descendants())
                    //    if (descendant is AutolinkInline || descendant is LinkInline)
                    //        descendant.GetAttributes().AddPropertyIfNotExist("target", "_blank");

                    var renderer = new HtmlRenderer(htmlWriter);
                    renderer.Render(document);

                    return htmlWriter.ToString();
                    //var sanitized = new HtmlSanitizer().Sanitize(rendered);
                    //return sanitized;
                }




                //return content;
            }
        }
    }
}
