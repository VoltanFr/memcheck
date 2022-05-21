using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MemCheck.CommandLineDbClient;

public sealed class TestRoleChecker : IRoleChecker
{
    public TestRoleChecker()
    {
    }
    public async Task<bool> UserIsAdminAsync(MemCheckDbContext dbContext, Guid userId)
    {
        await Task.CompletedTask;
        return false;
    }
    public async Task<bool> UserIsAdminAsync(MemCheckUser user)
    {
        await Task.CompletedTask;
        return false;
    }
    public async Task<IEnumerable<string>> GetRolesAsync(MemCheckUser user)
    {
        await Task.CompletedTask;
        return Array.Empty<string>();
    }
}

