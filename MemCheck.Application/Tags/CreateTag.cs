using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Tags
{
    public sealed class CreateTag
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public CreateTag(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task<Guid> RunAsync(Request request, ILocalized localizer)
        {
            await request.CheckValidityAsync(localizer, dbContext);
            Tag tag = new Tag() { Name = request.Name };
            dbContext.Tags.Add(tag);
            await dbContext.SaveChangesAsync();
            return tag.Id;
        }
        #region Request type
        public sealed record Request(string Name)
        {
            public const int MinNameLength = 3;
            public const int MaxNameLength = 50;
            private static readonly char[] forbiddenChars = new[] { '<', '>' };
            public async Task CheckValidityAsync(ILocalized localizer, MemCheckDbContext dbContext)
            {
                if (Name != Name.Trim())
                    throw new InvalidOperationException("Invalid Name: not trimmed");
                if (Name.Length < MinNameLength || Name.Length > MaxNameLength)
                    throw new RequestInputException(localizer.Get("InvalidNameLength") + $" {Name.Length}" + localizer.Get("MustBeBetween") + $" {MinNameLength} " + localizer.Get("And") + $" {MaxNameLength}");

                foreach (var forbiddenChar in forbiddenChars)
                    if (Name.Contains(forbiddenChar))
                        throw new RequestInputException(localizer.Get("InvalidTagName") + " '" + Name + "' ('" + forbiddenChar + ' ' + localizer.Get("IsForbidden") + ")");

                var exists = await dbContext.Tags.Where(tag => EF.Functions.Like(tag.Name, $"{Name}")).AnyAsync();
                if (exists)
                    throw new RequestInputException(localizer.Get("ATagWithName") + " '" + Name + "' " + localizer.Get("AlreadyExistsCaseInsensitive"));
            }
        }
        #endregion
    }
}
