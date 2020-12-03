using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Svg;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application
{
    internal interface IImageMetadata
    {
        public string Name { get; }
        public string Description { get; }
        public string Source { get; }
    }
    public sealed class StoreImage
    {
        #region Fields
        private const string svgImageContentType = "image/svg+xml";
        private readonly MemCheckDbContext dbContext;
        private readonly IStringLocalizer localizer;
        private static readonly ImmutableHashSet<string> supportedContentTypes = GetSupportedContentTypes();
        private const int bigImageWidth = 1600;
        #endregion
        #region Private methods
        private static ImmutableHashSet<string> GetSupportedContentTypes()
        {
            return new HashSet<string>(new string[] { svgImageContentType, "image/jpeg", "image/png", "image/gif" }).ToImmutableHashSet();
        }
        public byte[] ResizeImage(Bitmap originalImage, int targetWidth)
        {
            int targetheight = originalImage.Height * targetWidth / originalImage.Width;
            using (var resultImage = new Bitmap(targetWidth, targetheight))
            using (var resultImageGraphics = Graphics.FromImage(resultImage))
            {
                resultImageGraphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                resultImageGraphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                resultImageGraphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                resultImageGraphics.Clear(Color.White);
                resultImageGraphics.DrawImage(originalImage, 0, 0, targetWidth, targetheight);
                using (var targetStream = new MemoryStream())
                {
                    using (EncoderParameters encoderParameters = new EncoderParameters(1))
                    using (EncoderParameter encoderParameter = new EncoderParameter(Encoder.Quality, 80L))
                    {
                        encoderParameters.Param[0] = encoderParameter;
                        var jpegCodec = ImageCodecInfo.GetImageDecoders().First(codec => codec.FormatID == ImageFormat.Jpeg.Guid);
                        resultImage.Save(targetStream, jpegCodec, encoderParameters);
                    }
                    return targetStream.ToArray();
                }
            }
        }
        private SvgDocument LoadSvg(Stream sourceStream)
        {
            try
            {
                return SvgDocument.Open<SvgDocument>(sourceStream);
            }
            catch
            {
                throw new RequestInputException(localizer["SvgImageCanNotBeLoaded"].Value);
            }
        }
        private Bitmap GetBitmap(Stream sourceStream, string contentType)
        {
            if (contentType.Equals(svgImageContentType, StringComparison.OrdinalIgnoreCase))
            {
                var svgDocument = LoadSvg(sourceStream);
                return svgDocument.Draw(bigImageWidth, 0);
            }
            return new Bitmap(sourceStream);
        }
        #endregion
        public StoreImage(MemCheckDbContext dbContext, IStringLocalizer localizer)
        {
            this.dbContext = dbContext;
            this.localizer = localizer;
        }
        public async Task<Guid> RunAsync(Request request)
        {
            request.CheckValidity(localizer);

            if (dbContext.Images.Where(image => EF.Functions.Like(image.Name, request.Name)).Any())
                throw new RequestInputException(localizer["ImageAlreadyExists"] + $" '{request.Name}'");

            var image = new Domain.Image();
            image.Name = request.Name;
            image.Description = request.Description;
            image.Source = request.Source;
            image.Owner = request.Owner;
            image.VersionDescription = localizer["InitialImageVersionCreation"].Value;
            image.VersionType = ImageVersionType.Creation;
            image.InitialUploadUtcDate = DateTime.UtcNow;
            image.LastChangeUtcDate = image.InitialUploadUtcDate;

            image.OriginalContentType = request.ContentType;
            image.OriginalSize = request.Blob.Length;
            image.OriginalBlob = request.Blob;

            using (var sourceStream = new MemoryStream(request.Blob))
            {
                using (var originalImage = GetBitmap(sourceStream, request.ContentType))
                {
                    image.SmallBlob = ResizeImage(originalImage, 100);
                    image.SmallBlobSize = image.SmallBlob.Length;
                    image.MediumBlob = ResizeImage(originalImage, 600);
                    image.MediumBlobSize = image.MediumBlob.Length;
                    image.BigBlob = ResizeImage(originalImage, bigImageWidth);
                    image.BigBlobSize = image.BigBlob.Length;
                    dbContext.Images.Add(image);
                    await dbContext.SaveChangesAsync();
                    return image.Id;
                }
            }
        }
        #region Request class
        public sealed class Request : IImageMetadata
        {
            #region Fields
            private const int minBlobLength = 10;
            private const int maxBlobLength = 10000000;
            #endregion
            public Request(MemCheckUser owner, string name, string description, string source, string contentType, byte[] blob)
            {
                Name = name.Trim();
                Description = description.Trim();
                Source = source.Trim();
                ContentType = contentType.Trim();
                Owner = owner;
                Blob = blob;
            }
            public MemCheckUser Owner { get; }
            public string Name { get; }
            public string Description { get; }
            public string Source { get; }
            public string ContentType { get; }
            public byte[] Blob { get; }
            public void CheckValidity(IStringLocalizer localizer)
            {
                ImageMetadataInputValidator.Run(this, localizer);
                if (!supportedContentTypes.Contains(ContentType))
                    throw new RequestInputException(localizer["InvalidImageContentType"] + $" '{ContentType}'");
                if (Blob.Length < minBlobLength || Blob.Length > maxBlobLength)
                    throw new RequestInputException(localizer["InvalidBlobLength"] + $" {Blob.Length}" + localizer["MustBeBetween"] + $" {minBlobLength} " + localizer["And"] + $" {maxBlobLength}");
            }
        }
        #endregion
    }
}
