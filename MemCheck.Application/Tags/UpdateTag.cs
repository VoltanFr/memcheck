using MemCheck.Application.QueryValidation;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace MemCheck.Application.Tags;

public sealed class UpdateTag : RequestRunner<UpdateTag.Request, UpdateTag.Result>
{
    #region Fields
    private readonly DateTime? runDate;
    #endregion
    public UpdateTag(CallContext callContext, DateTime? runDate = null) : base(callContext)
    {
        this.runDate = runDate;
    }
    protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
    {
        var tag = await DbContext.Tags
            .Include(tag => tag.PreviousVersion)
            .Include(tag => tag.CreatingUser)
            .SingleAsync(tag => tag.Id == request.TagId);
        var creatingUser = await DbContext.Users.SingleAsync(user => user.Id == request.UserId);

        var versionFromCurrentTag = new TagPreviousVersion()
        {
            Tag = request.TagId,
            Name = tag.Name,
            Description = tag.Description,
            VersionUtcDate = tag.VersionUtcDate,
            CreatingUser = tag.CreatingUser,
            VersionType = tag.VersionType,
            VersionDescription = tag.VersionDescription,
            PreviousVersion = tag.PreviousVersion,
        };

        DbContext.TagPreviousVersions.Add(versionFromCurrentTag);

        var initialName = tag.Name;
        tag.Name = request.NewName;
        tag.Description = request.NewDescription;
        tag.VersionUtcDate = runDate ?? DateTime.UtcNow;
        tag.CreatingUser = creatingUser;
        tag.VersionDescription = request.VersionDescription;
        tag.VersionType = TagVersionType.Changes;
        tag.PreviousVersion = versionFromCurrentTag;

        await DbContext.SaveChangesAsync();

        return new ResultWithMetrologyProperties<Result>(new Result(), ("InitialName", initialName), ("NewName", request.NewName), ("NewDescription", request.NewDescription));
    }
    #region Request & Result
    public sealed record Request(Guid UserId, Guid TagId, string NewName, string NewDescription, string VersionDescription) : IRequest
    {
        public async Task CheckValidityAsync(CallContext callContext)
        {
            await QueryValidationHelper.CheckUserExistsAsync(callContext.DbContext, UserId);
            await QueryValidationHelper.CheckCanCreateTag(NewName, NewDescription, TagId, callContext.DbContext, callContext.Localized, callContext.RoleChecker, UserId, VersionDescription);
        }
    }
    public sealed record Result();
    #endregion
}
