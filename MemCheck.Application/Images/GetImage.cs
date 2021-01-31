using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Images
{
    public sealed class GetImage
    {
        #region Fields
        private readonly MemCheckDbContext dbContext;
        #endregion
        public GetImage(MemCheckDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task<byte[]> RunAsync(Request request)
        {
            return request.Size switch
            {
                Request.ImageSize.Small => (await dbContext.Images.AsNoTracking().Select(img => new { img.Id, img.SmallBlob }).SingleAsync(img => img.Id == request.ImageId)).SmallBlob,
                Request.ImageSize.Medium => (await dbContext.Images.AsNoTracking().Select(img => new { img.Id, img.MediumBlob }).SingleAsync(img => img.Id == request.ImageId)).MediumBlob,
                Request.ImageSize.Big => (await dbContext.Images.AsNoTracking().Select(img => new { img.Id, img.BigBlob }).SingleAsync(img => img.Id == request.ImageId)).BigBlob,
                _ => throw new NotImplementedException(request.Size.ToString()),
            };
        }
        #region Request class
        public sealed record Request(Guid ImageId, Request.ImageSize Size)
        {
            public enum ImageSize { Small, Medium, Big };
        }
        #endregion
    }
}
