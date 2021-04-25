using MemCheck.Application.QueryValidation;
using MemCheck.Basics;
using MemCheck.Domain;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace MemCheck.Application.Tests.Helpers
{
    public sealed class TestRoleChecker : IRoleChecker
    {
        #region Fields
        private readonly ImmutableHashSet<Guid> admins;
        #endregion
        #region Private method
        #endregion
        public TestRoleChecker(params Guid[] admins)
        {
            this.admins = admins.ToImmutableHashSet();
        }
        public async Task<bool> UserIsAdminAsync(MemCheckUser user)
        {
            await Task.CompletedTask;
            return admins.Contains(user.Id);
        }
        public async Task<IEnumerable<string>> GetRolesAsync(MemCheckUser user)
        {
            await Task.CompletedTask;
            if (await UserIsAdminAsync(user))
                return IRoleChecker.AdminRoleName.AsArray();

            return Array.Empty<string>();
        }
    }
}
