using MemCheck.Application.QueryValidation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace MemCheck.Application.Tags
{
    public sealed class UpdateTag : RequestRunner<UpdateTag.Request, UpdateTag.Result>
    {
        public UpdateTag(CallContext callContext) : base(callContext)
        {
        }
        protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
        {
            var tag = await DbContext.Tags.SingleAsync(tag => tag.Id == request.TagId);
            var initialName = tag.Name;
            tag.Name = request.NewName;
            tag.Description = request.NewDescription;
            await DbContext.SaveChangesAsync();
            return new ResultWithMetrologyProperties<Result>(new Result(), ("InitialName", initialName), ("NewName", request.NewName), ("NewDescription", request.NewDescription));
        }
        #region Request & Result
        public sealed record Request(Guid UserId, Guid TagId, string NewName, string NewDescription) : IRequest
        {
            public async Task CheckValidityAsync(CallContext callContext)
            {
                await QueryValidationHelper.CheckUserExistsAsync(callContext.DbContext, UserId);
                await QueryValidationHelper.CheckCanCreateTag(NewName, NewDescription, TagId, callContext.DbContext, callContext.Localized);
            }
        }
        public sealed record Result();
        #endregion
    }
}
