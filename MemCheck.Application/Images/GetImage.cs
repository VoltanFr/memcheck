using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Images;

public sealed class GetImage : RequestRunner<GetImage.Request, GetImage.Result>
{
    #region Fields
    #endregion
    public GetImage(CallContext callContext) : base(callContext)
    {
    }
    protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
    {
        byte[] result = request.Size switch
        {
            Request.ImageSize.Small => (await DbContext.Images.AsNoTracking().Select(img => new { img.Id, img.SmallBlob }).SingleAsync(img => img.Id == request.ImageId)).SmallBlob,
            Request.ImageSize.Medium => (await DbContext.Images.AsNoTracking().Select(img => new { img.Id, img.MediumBlob }).SingleAsync(img => img.Id == request.ImageId)).MediumBlob,
            Request.ImageSize.Big => (await DbContext.Images.AsNoTracking().Select(img => new { img.Id, img.BigBlob }).SingleAsync(img => img.Id == request.ImageId)).BigBlob,
            _ => throw new NotImplementedException(request.Size.ToString()),
        };
        return new ResultWithMetrologyProperties<Result>(new Result(result.ToImmutableArray()), ("ImageId", request.ImageId.ToString()), ("RequestedSize", request.Size.ToString()), IntMetric("ByteCount", result.Length));
    }
    #region Request class
    public sealed record Request(Guid ImageId, Request.ImageSize Size) : IRequest
    {
        public async Task CheckValidityAsync(CallContext callContext)
        {
            await Task.CompletedTask;
        }
        public enum ImageSize { Small, Medium, Big };
    }
    public sealed record Result(ImmutableArray<byte> ImageBytes);
    #endregion
}
