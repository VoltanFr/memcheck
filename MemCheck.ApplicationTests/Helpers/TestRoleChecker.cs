using MemCheck.Application.QueryValidation;
using MemCheck.Basics;
using MemCheck.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MemCheck.Application.Tests.Helpers
{
    public sealed class TestRoleChecker : IRoleChecker
    {
        #region Fields
        private readonly bool adminReply;
        private readonly IEnumerable<string> rolesReply;
        private static readonly TestRoleChecker trueForAdmin = new TestRoleChecker(true, IRoleChecker.AdminRoleName.AsArray());
        private static readonly TestRoleChecker falseForAdmin = new TestRoleChecker(false, System.Array.Empty<string>());
        #endregion
        #region Private method
        private TestRoleChecker(bool adminReply, IEnumerable<string> rolesReply)
        {
            this.adminReply = adminReply;
            this.rolesReply = rolesReply;
        }
        #endregion
        public async Task<bool> UserIsAdminAsync(MemCheckUser user)
        {
            await Task.CompletedTask;
            return adminReply;
        }
        public async Task<IEnumerable<string>> GetRolesAsync(MemCheckUser user)
        {
            await Task.CompletedTask;
            return rolesReply;
        }
        public static TestRoleChecker TrueForAdmin => trueForAdmin;
        public static TestRoleChecker FalseForAdmin => falseForAdmin;
    }
}
