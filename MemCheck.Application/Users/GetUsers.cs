using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Users
{
    public sealed class GetUsers
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public GetUsers(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task<IEnumerable<ViewModel>> RunAsync()
        {
            return await dbContext.Users.AsNoTracking().Select(user => new ViewModel(user.Id, user.UserName)).ToListAsync();
        }
        #region Result type
        public sealed class ViewModel
        {
            public ViewModel()
            {
            }
            public ViewModel(Guid userId, string userName)
            {
                UserId = userId;
                UserName = userName;
            }
            public Guid UserId { get; set; }
            public string UserName { get; set; } = null!;
        }
        #endregion
    }
}
