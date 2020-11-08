using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MemCheck.Application
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
        public IEnumerable<ViewModel> Run()
        {
            return dbContext.Users.AsNoTracking().Select(user => new ViewModel(user.Id, user.UserName)).ToList();
        }

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
    }
}
