using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Images
{
    public sealed class GetImage
    {
        #region Fields
        private readonly CallContext callContext;
        #endregion
        public GetImage(CallContext callContext)
        {
            this.callContext = callContext;
        }
        public async Task<byte[]> RunAsync(Request request)
        {
            byte[] result = request.Size switch
            {
                Request.ImageSize.Small => (await callContext.DbContext.Images.AsNoTracking().Select(img => new { img.Id, img.SmallBlob }).SingleAsync(img => img.Id == request.ImageId)).SmallBlob,
                Request.ImageSize.Medium => (await callContext.DbContext.Images.AsNoTracking().Select(img => new { img.Id, img.MediumBlob }).SingleAsync(img => img.Id == request.ImageId)).MediumBlob,
                Request.ImageSize.Big => (await callContext.DbContext.Images.AsNoTracking().Select(img => new { img.Id, img.BigBlob }).SingleAsync(img => img.Id == request.ImageId)).BigBlob,
                _ => throw new NotImplementedException(request.Size.ToString()),
            };
            callContext.TelemetryClient.TrackEvent("GetImage", ("ImageId", request.ImageId.ToString()), ("RequestedSize", request.Size.ToString()), ("ByteCount", result.Length.ToString()));
            return result;
        }
        #region Request class
        public sealed record Request(Guid ImageId, Request.ImageSize Size)
        {
            public enum ImageSize { Small, Medium, Big };
        }
        #endregion
    }
}
