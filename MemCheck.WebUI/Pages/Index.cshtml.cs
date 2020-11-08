using System.Collections.Generic;
using System.Threading.Tasks;
using MemCheck.Application;
using MemCheck.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace MemCheck.WebUI.Pages
{
    public class IndexModel : PageModel
    {
        #region Fields
        private readonly ILogger<IndexModel> _logger;
        private readonly MemCheckDbContext dbContext;
        #endregion

        public IndexModel(ILogger<IndexModel> logger, MemCheckDbContext dbContext)
        {
            _logger = logger;
            this.dbContext = dbContext;
        }
        public void OnGet()
        {
            Cards = new GetAllCardsInDb(dbContext).Run();
        }
        //public async Task<IActionResult> OnPost()
        //{
        //    await new CreateCard(dbContext).RunAsync(Card);
        //    return RedirectToPage("Index");
        //}
        public IEnumerable<GetAllCardsInDb.ViewModel> Cards { get; set; } = null!;
        [BindProperty] public CreateCard.Request Card { get; set; } = null!;
    }
}
