using MemCheck.Basics;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Tags;

/* Reports all tags which have been modified since request.SinceUtcDate
 * The result includes all previous versions since request.SinceUtcDate, plus an additional one if any (so that we can report the version before changes)
 */
public sealed class GetModifiedTags : RequestRunner<GetModifiedTags.Request, GetModifiedTags.Result>
{
    public GetModifiedTags(CallContext callContext) : base(callContext)
    {
    }
    protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
    {
        //Since there are not a lot of tags, we just load them all into RAM

        var tagsFromDb = await DbContext.Tags
            .AsNoTracking()
            .Include(tag => tag.CreatingUser)
            .Include(tag => tag.PreviousVersion)
            .Where(tag => tag.VersionUtcDate >= request.SinceUtcDate)
            .ToImmutableArrayAsync();

        var resultTags = new List<ResultTag>();

        foreach (var tagFromDb in tagsFromDb)
        {
            var liveVersion = new TagVersion(tagFromDb.CreatingUser.GetUserName(), tagFromDb.Name, tagFromDb.Description, tagFromDb.VersionDescription, tagFromDb.VersionUtcDate, tagFromDb.VersionType);

            var allVersions = await DbContext.TagPreviousVersions
                .AsNoTracking()
                .Include(tagPreviousVersion => tagPreviousVersion.CreatingUser)
                .Include(tagPreviousVersion => tagPreviousVersion.PreviousVersion)
                .Where(tagPreviousVersion => tagPreviousVersion.Tag == tagFromDb.Id && tagPreviousVersion.VersionUtcDate >= request.SinceUtcDate)
                .ToImmutableDictionaryAsync(tagPreviousVersion => tagPreviousVersion.Id, tagPreviousVersion => tagPreviousVersion);

            var resultVersions = new List<TagVersion> { liveVersion };

            var currentVersion = tagFromDb.PreviousVersion;
            while (currentVersion != null && allVersions.ContainsKey(currentVersion.Id))
            {
                currentVersion = allVersions[currentVersion.Id];
                resultVersions.Add(new TagVersion(currentVersion.CreatingUser.GetUserName(), currentVersion.Name, currentVersion.Description, currentVersion.VersionDescription, currentVersion.VersionUtcDate, currentVersion.VersionType));
                currentVersion = currentVersion.PreviousVersion;
            }

            if (currentVersion != null)
            {
                // Let's get the version before the oldest version in the date range
                var olderVersion = await DbContext.TagPreviousVersions
                    .AsNoTracking()
                    .Include(tagPreviousVersion => tagPreviousVersion.CreatingUser)
                    .Include(tagPreviousVersion => tagPreviousVersion.PreviousVersion)
                    .SingleAsync(tagPreviousVersion => tagPreviousVersion.Id == currentVersion.Id);
                resultVersions.Add(new TagVersion(olderVersion.CreatingUser.GetUserName(), olderVersion.Name, olderVersion.Description, olderVersion.VersionDescription, olderVersion.VersionUtcDate, olderVersion.VersionType));
            }

            var resultTag = new ResultTag(tagFromDb.Id, tagFromDb.CountOfPublicCards, tagFromDb.AverageRatingOfPublicCards, resultVersions.ToImmutableArray());
            resultTags.Add(resultTag);
        }

        var result = new Result(resultTags.ToImmutableArray());

        return new ResultWithMetrologyProperties<Result>(result, IntMetric("TagCount", result.Tags.Length));
    }
    #region Request & Result
    public sealed record Request(DateTime SinceUtcDate) : IRequest
    {
        public async Task CheckValidityAsync(CallContext callContext)
        {
            await Task.CompletedTask;
        }
    }
    public sealed record Result(ImmutableArray<ResultTag> Tags);
    public sealed record ResultTag(Guid TagId, int CountOfPublicCards, double AverageRatingOfPublicCards, ImmutableArray<TagVersion> Versions);
    public sealed record TagVersion(string CreatorName, string TagName, string Description, string VersionDescription, DateTime UtcDate, TagVersionType VersionType);
    #endregion
}
