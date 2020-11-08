using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using Svg;
using System;
using System.Drawing;
using System.IO;
using System.Linq;

namespace MemCheck.Application
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
        public byte[] Run(Request request)
        {
            request.CheckValidity();

            //This implementation takes care of not loading all sizes :-)
            var images = dbContext.Images.AsNoTracking().Where(image => image.Id == request.ImageId).ToList();

            if (images.Count != 1)
                throw new RequestInputException($"Unknown image (dbcount={images.Count()})");

            var image = images[0];

            if (request.Size == 3)
                return image.BigBlob;

            if (request.Size == 2)
                return image.MediumBlob;

            return image.SmallBlob;
        }
        #region Request class
        public sealed class Request
        {
            public Request(Guid imageId, int size)
            {
                ImageId = imageId;
                Size = size;
            }
            public Guid ImageId { get; }
            public int Size { get; }
            public void CheckValidity()
            {
                if (QueryValidationHelper.IsReservedGuid(ImageId))
                    throw new RequestInputException($"Invalid image id {ImageId}");
                if (Size < 1 || Size > 3)
                    throw new RequestInputException($"Invalid image size {Size}");
            }
        }
        #endregion
    }
}
