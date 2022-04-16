using MemCheck.Application.QueryValidation;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Images
{
    public sealed class GetImageInfoFromId : RequestRunner<GetImageInfoFromId.Request, GetImageInfoFromId.Result>
    {
        #region Fields
        #endregion
        public GetImageInfoFromId(CallContext callContext) : base(callContext)
        {
        }
        protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
        {
            var result = await DbContext.Images
                            .AsNoTracking()
                            .Where(img => img.Id == request.ImageId)
                            .Select(img => new Result(img.Owner, img.Name, img.Description, img.Source, img.Cards.Count(), img.InitialUploadUtcDate, img.LastChangeUtcDate, img.VersionDescription))
                            .SingleAsync();
            return new ResultWithMetrologyProperties<Result>(result, ("ImageId", request.ImageId.ToString()));
        }
        #region Request and Result
        public sealed record Request(Guid ImageId) : IRequest
        {
            public async Task CheckValidityAsync(CallContext callContext)
            {
                QueryValidationHelper.CheckNotReservedGuid(ImageId);
                await Task.CompletedTask;
            }
        }
        public sealed record Result(MemCheckUser Owner, string Name, string Description, string Source, int CardCount, DateTime InitialUploadUtcDate, DateTime LastChangeUtcDate, string CurrentVersionDescription);
        #endregion
    }
}
