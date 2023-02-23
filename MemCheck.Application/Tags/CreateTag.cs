using MemCheck.Application.QueryValidation;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace MemCheck.Application.Tags;

public sealed class CreateTag : RequestRunner<CreateTag.Request, CreateTag.Result>
{
    #region Fields
    private readonly DateTime? runDate;
    #endregion
    public CreateTag(CallContext callContext, DateTime? runDate = null) : base(callContext)
    {
        this.runDate = runDate;
    }
    protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
    {
        var user = await DbContext.Users.SingleAsync(u => u.Id == request.UserId);
        Tag tag = new() { Name = request.Name, CreatingUser = user, Description = request.Description, VersionDescription = request.VersionDescription, VersionUtcDate = runDate ?? DateTime.UtcNow };
        DbContext.Tags.Add(tag);
        await DbContext.SaveChangesAsync();
        return new ResultWithMetrologyProperties<Result>(new Result(tag.Id), IntMetric("NameLength", request.Name.Length), IntMetric("DescriptionLength", request.Description.Length));
    }
    #region Request & Result
    public sealed record Request(Guid UserId, string Name, string Description, string VersionDescription) : IRequest
    {
        public async Task CheckValidityAsync(CallContext callContext)
        {
            await QueryValidationHelper.CheckUserExistsAsync(callContext.DbContext, UserId);
            await QueryValidationHelper.CheckCanCreateTag(Name, Description, null, callContext.DbContext, callContext.Localized, callContext.RoleChecker, UserId, VersionDescription);
        }
    }
    public sealed record Result(Guid TagId);
    #endregion
}
