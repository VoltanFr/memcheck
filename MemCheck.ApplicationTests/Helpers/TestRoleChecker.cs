using MemCheck.Application.Languages;
using MemCheck.Domain;
using System.Threading.Tasks;

namespace MemCheck.Application.Tests.Helpers
{
    public sealed class TestRoleChecker : IRoleChecker
    {
        #region Fields
        private readonly bool adminReply;
        private static readonly TestRoleChecker trueForAdmin = new TestRoleChecker(true);
        private static readonly TestRoleChecker falseForAdmin = new TestRoleChecker(false);
        #endregion
        private TestRoleChecker(bool adminReply)
        {
            this.adminReply = adminReply;
        }
        public async Task<bool> UserIsAdminAsync(MemCheckUser user)
        {
            await Task.CompletedTask;
            return adminReply;
        }
        public static TestRoleChecker TrueForAdmin => trueForAdmin;
        public static TestRoleChecker FalseForAdmin => falseForAdmin;
    }
}
