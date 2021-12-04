using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Users
{
    public sealed class GetUsers : RequestRunner<GetUsers.Request, IEnumerable<GetUsers.ViewModel>>
    {
        public GetUsers(CallContext callContext) : base(callContext)
        {
        }
        protected override async Task<ResultWithMetrologyProperties<IEnumerable<ViewModel>>> DoRunAsync(Request request)
        {
            var result = await DbContext.Users.AsNoTracking().Select(user => new ViewModel(user.Id, user.UserName)).ToListAsync();
            return new ResultWithMetrologyProperties<IEnumerable<ViewModel>>(result, ("ResultCount", result.Count.ToString()));
        }
        #region Request & Result
        public sealed record Request() : IRequest
        {
            public async Task CheckValidityAsync(CallContext callContext)
            {
                await Task.CompletedTask;
            }
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
        #endregion
    }
}
