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
        private const int minLength = 3;
        private const int maxLength = 50;
        private readonly MemCheckDbContext dbContext;
        private static readonly char[] forbiddenChars = new[] { '<', '>' };
        #endregion
        public CreateTag(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task<Guid> RunAsync(string name)
        {
            name = name.Trim();
            CheckNameValidity(dbContext, name);

            Tag tag = new Tag() { Name = name };
            dbContext.Tags.Add(tag);
            await dbContext.SaveChangesAsync();

            return tag.Id;
        }
        public static void CheckNameValidity(MemCheckDbContext dbContext, string name)
        {
            if (name.Length < minLength || name.Length > maxLength)
                throw new InvalidOperationException($"Invalid tag name '{name}' (length must be between {minLength} and {maxLength})");

            foreach (var forbiddenChar in forbiddenChars)
                if (name.Contains(forbiddenChar))
                    throw new InvalidOperationException($"Invalid tag name '{name}' (length must be between {minLength} and {maxLength})");

            var exists = dbContext.Tags.Where(tag => EF.Functions.Like(tag.Name, $"{name}")).Any();
            if (exists)
                throw new InvalidOperationException($"A tag with name '{name}' already exists (tags are case insensitive)");
        }
    }
}
