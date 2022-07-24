using MemCheck.Application.QueryValidation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Images;

public sealed class GetImageFromName : RequestRunner<GetImageFromName.Request, GetImageFromName.Result>
{
    #region Fields
    #endregion
    public GetImageFromName(CallContext callContext) : base(callContext)
    {
    }
    protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
    {
        var result = request.Size switch
        {
            Request.ImageSize.Small => (await DbContext.Images.AsNoTracking().Select(img => new { img.Name, img.SmallBlob }).SingleAsync(img => img.Name == request.ImageName)).SmallBlob,
            Request.ImageSize.Medium => (await DbContext.Images.AsNoTracking().Select(img => new { img.Name, img.MediumBlob }).SingleAsync(img => img.Name == request.ImageName)).MediumBlob,
            Request.ImageSize.Big => (await DbContext.Images.AsNoTracking().Select(img => new { img.Name, img.BigBlob }).SingleAsync(img => img.Name == request.ImageName)).BigBlob,
            _ => throw new NotImplementedException(request.Size.ToString()),
        };
        return new ResultWithMetrologyProperties<Result>(new Result(result.ToImmutableArray()), ("ImageName", request.ImageName), ("RequestedSize", request.Size.ToString()), IntMetric("ByteCount", result.Length));
    }
    #region Request class
    public sealed record Request(string ImageName, Request.ImageSize Size) : IRequest
    {
        public async Task CheckValidityAsync(CallContext callContext)
        {
            QueryValidationHelper.CheckImageNameValidity(ImageName, callContext.Localized);
            await Task.CompletedTask;
        }
        public enum ImageSize { Small, Medium, Big };
    }
    public sealed record Result(ImmutableArray<byte> ImageBytes);
    #endregion
}
