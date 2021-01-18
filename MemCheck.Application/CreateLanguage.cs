using MemCheck.Database;
using MemCheck.Domain;
using System;
using System.Threading.Tasks;

namespace MemCheck.Application
{
    public sealed class CreateLanguage
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public CreateLanguage(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task<GetAllLanguages.ViewModel> RunAsync(Request request)
        {
            if (request.Name.Length < 4)
                throw new InvalidOperationException($"Invalid language name '{request.Name}'");
            var language = new CardLanguage() { Name = request.Name };
            dbContext.CardLanguages.Add(language);
            await dbContext.SaveChangesAsync();

            return new GetAllLanguages.ViewModel(language.Id, language.Name, 0);
        }
        public sealed class Request
        {
            public string Name { get; set; } = null!;
        }
    }
}
