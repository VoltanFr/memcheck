﻿using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace MemCheck.Application
{
    public sealed class GetAllAvailableTags
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public GetAllAvailableTags(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public static ImmutableDictionary<Guid, string> Run(MemCheckDbContext dbContext)
        {
            return dbContext.Tags.AsNoTracking().Select(t => new { t.Id, t.Name }).ToImmutableDictionary(t => t.Id, t => t.Name);
        }
        public IEnumerable<ViewModel> Run()
        {
            return dbContext.Tags.AsNoTracking().OrderBy(tag => tag.Name).Select(tag => new ViewModel() { TagId = tag.Id, Name = tag.Name }).ToList();
        }
        public sealed class ViewModel
        {
            //This class is also used as a request model in SearchCards.SearchRequest, that's why properties are get & set
            public Guid TagId { get; set; }
            public string Name { get; set; } = null!;
        }
    }
}