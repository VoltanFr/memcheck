using MemCheck.Application.QueryValidation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace MemCheck.Application.Users
{
    public sealed class UpdateUserLastSeenDate : RequestRunner<UpdateUserLastSeenDate.Request, UpdateUserLastSeenDate.Result>
    {
        #region Fields
        private readonly DateTime? runDate;
        #endregion
        public UpdateUserLastSeenDate(CallContext callContext, DateTime? runDate = null) : base(callContext)
        {
            this.runDate = runDate;
        }
        protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
        {
            var user = await DbContext.Users.SingleAsync(user => user.Id == request.UserId);
            user.LastSeenUtcDate = runDate ?? DateTime.UtcNow;
            await DbContext.SaveChangesAsync();
            return new ResultWithMetrologyProperties<Result>(new Result());
        }
        #region Request & Result
        public sealed record Request(Guid UserId) : IRequest
        {
            public async Task CheckValidityAsync(CallContext callContext)
            {
                await QueryValidationHelper.CheckUserExistsAsync(callContext.DbContext, UserId);
            }
        }
        public sealed record Result();
        #endregion
    }
}
