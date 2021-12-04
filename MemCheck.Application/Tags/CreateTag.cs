using MemCheck.Application.QueryValidation;
using MemCheck.Domain;
using System;
using System.Threading.Tasks;

namespace MemCheck.Application.Tags
{
    public sealed class CreateTag : RequestRunner<CreateTag.Request, CreateTag.Result>
    {
        public CreateTag(CallContext callContext) : base(callContext)
        {
        }
        protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
        {
            Tag tag = new() { Name = request.Name, Description = request.Description };
            DbContext.Tags.Add(tag);
            await DbContext.SaveChangesAsync();
            return new ResultWithMetrologyProperties<Result>(new Result(tag.Id), ("NameLength", request.Name.Length.ToString()), ("DescriptionLength", request.Description.Length.ToString()));
        }
        #region Request & Result
        public sealed record Request(Guid UserId, string Name, string Description) : IRequest
        {
            public async Task CheckValidityAsync(CallContext callContext)
            {
                await QueryValidationHelper.CheckUserExistsAsync(callContext.DbContext, UserId);
                await QueryValidationHelper.CheckCanCreateTag(Name, Description, null, callContext.DbContext, callContext.Localized);
            }
        }
        public sealed record Result(Guid TagId);
        #endregion
    }
}
