﻿using Microsoft.EntityFrameworkCore;
using MemCheck.Database;
using MemCheck.Domain;
using System.Threading.Tasks;
using System;

namespace MemCheck.Application.Tests.Notifying
{
    public static class TagHelper
    {
        public static async Task<Guid> CreateAsync(DbContextOptions<MemCheckDbContext> testDB)
        {
            using var dbContext = new MemCheckDbContext(testDB);
            var result = new Tag();
            result.Name = StringServices.RandomString();
            dbContext.Tags.Add(result);
            await dbContext.SaveChangesAsync();
            return result.Id;
        }
    }
}