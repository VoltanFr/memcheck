﻿using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using System;
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
            Tag tag = new Tag() { Name = request.Name, Description = request.Description };
            dbContext.Tags.Add(tag);
            await dbContext.SaveChangesAsync();
            return tag.Id;
        }
        #region Request type
        public sealed record Request(string Name, string Description)
        {
            public async Task CheckValidityAsync(ILocalized localizer, MemCheckDbContext dbContext)
            {
                await QueryValidationHelper.CheckCanCreateTag(Name, Description, null, dbContext, localizer);
            }
        }
        #endregion
    }
}
