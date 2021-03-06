﻿using MemCheck.Application.QueryValidation;
using MemCheck.Application.Tests.Helpers;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Tags
{
    [TestClass()]
    public class CreateTagTests
    {
        [TestMethod()]
        public async Task EmptyName()
        {
            using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new CreateTag(dbContext).RunAsync(new CreateTag.Request("", ""), new TestLocalizer()));
        }
        [TestMethod()]
        public async Task NameNotTrimmed()
        {
            using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new CreateTag(dbContext).RunAsync(new CreateTag.Request(RandomHelper.String(Tag.MinNameLength) + '\t', ""), new TestLocalizer()));
        }
        [TestMethod()]
        public async Task DescriptionNotTrimmed()
        {
            using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await new CreateTag(dbContext).RunAsync(new CreateTag.Request(RandomHelper.String(), RandomHelper.String() + '\t'), new TestLocalizer()));
        }
        [TestMethod()]
        public async Task NameTooShort()
        {
            using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new CreateTag(dbContext).RunAsync(new CreateTag.Request(RandomHelper.String(Tag.MinNameLength - 1), ""), new TestLocalizer()));
        }
        [TestMethod()]
        public async Task NameTooLong()
        {
            using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new CreateTag(dbContext).RunAsync(new CreateTag.Request(RandomHelper.String(Tag.MaxNameLength + 1), ""), new TestLocalizer()));
        }
        [TestMethod()]
        public async Task DescriptionTooLong()
        {
            using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new CreateTag(dbContext).RunAsync(new CreateTag.Request(RandomHelper.String(), RandomHelper.String(Tag.MaxDescriptionLength + 1)), new TestLocalizer()));
        }
        [TestMethod()]
        public async Task NameWithForbiddenChar()
        {
            using var dbContext = new MemCheckDbContext(DbHelper.GetEmptyTestDB());
            await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new CreateTag(dbContext).RunAsync(new CreateTag.Request("a<b", ""), new TestLocalizer()));
        }
        [TestMethod()]
        public async Task AlreadyExists()
        {
            var name = RandomHelper.String();
            var db = DbHelper.GetEmptyTestDB();
            using (var dbContext = new MemCheckDbContext(db))
                await new CreateTag(dbContext).RunAsync(new CreateTag.Request(name, ""), new TestLocalizer());
            using (var dbContext = new MemCheckDbContext(db))
                await Assert.ThrowsExceptionAsync<RequestInputException>(async () => await new CreateTag(dbContext).RunAsync(new CreateTag.Request(name, ""), new TestLocalizer()));
        }
        [TestMethod()]
        public async Task Success()
        {
            var name = RandomHelper.String();
            var description = RandomHelper.String();
            var db = DbHelper.GetEmptyTestDB();
            Guid tag;
            using (var dbContext = new MemCheckDbContext(db))
                tag = await new CreateTag(dbContext).RunAsync(new CreateTag.Request(name, description), new TestLocalizer());
            using (var dbContext = new MemCheckDbContext(db))
            {
                var loaded = await new GetTag(dbContext).RunAsync(new GetTag.Request(tag));
                Assert.AreEqual(name, loaded.TagName);
                Assert.AreEqual(description, loaded.Description);
            }
        }
    }
}
