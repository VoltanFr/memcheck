using Markdig;
using Markdig.Extensions.AutoLinks;
using Markdig.Renderers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IO;
using System.Linq;

namespace MemCheck.WebUI.Pages.Doc
{
    public sealed class MdRendererModel : PageModel
    {
        private readonly IWebHostEnvironment environment;
        public MdRendererModel(IWebHostEnvironment environment)
        {
            this.environment = environment;
        }
        [BindProperty(SupportsGet = true)] public string RefererRoute { get; set; } = "";
        [BindProperty(SupportsGet = true)] public string CultureName { get; set; } = "";
        public string PageContents
        {
            get
            {
                var baseDir = Path.Combine(environment.ContentRootPath, "wwwroot", "mddoc");
                var splittedRefererRoute = RefererRoute.Split('/');
                var refererPath = Path.Combine(splittedRefererRoute.SkipLast(1).ToArray());
                var refererPage = splittedRefererRoute.Last();
                var filePath = Path.Combine(baseDir, refererPath, $"{refererPage}-{CultureName}.md");
                if (!System.IO.File.Exists(filePath))
                {
                    filePath = Path.Combine(baseDir, $"index-{CultureName}.md");
                    if (!System.IO.File.Exists(filePath))
                        filePath = Path.Combine(baseDir, "index-en.md");
                }
                var markdown = System.IO.File.ReadAllText(filePath);



                using var htmlWriter = new StringWriter();
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




                //return content;
            }
        }
    }
}
