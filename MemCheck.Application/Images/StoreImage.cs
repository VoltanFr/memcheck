using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using Svg;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Images
{
    public sealed class StoreImage
    {
        #region Fields
        public const string svgImageContentType = "image/svg+xml";
        public const string pngImageContentType = "image/png";
        private readonly CallContext callContext;
        private static readonly ImmutableHashSet<string> supportedContentTypes = GetSupportedContentTypes();
        private const int bigImageWidth = 1600;
        #endregion
        #region Private methods
        private static ImmutableHashSet<string> GetSupportedContentTypes()
        {
            return new HashSet<string>(new[] { svgImageContentType, "image/jpeg", pngImageContentType, "image/gif" }).ToImmutableHashSet();
        }
        public static byte[] ResizeImage(Bitmap originalImage, int targetWidth)
        {
            int targetheight = originalImage.Height * targetWidth / originalImage.Width;
            using var resultImage = new Bitmap(targetWidth, targetheight);
            using var resultImageGraphics = Graphics.FromImage(resultImage);
            resultImageGraphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            resultImageGraphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            resultImageGraphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            resultImageGraphics.Clear(Color.White);
            resultImageGraphics.DrawImage(originalImage, 0, 0, targetWidth, targetheight);
            using var targetStream = new MemoryStream();
            using EncoderParameters encoderParameters = new(1);
            using EncoderParameter encoderParameter = new(Encoder.Quality, 80L);
            encoderParameters.Param[0] = encoderParameter;
            var jpegCodec = ImageCodecInfo.GetImageDecoders().First(codec => codec.FormatID == ImageFormat.Jpeg.Guid);
            resultImage.Save(targetStream, jpegCodec, encoderParameters);
            return targetStream.ToArray();
        }
        private SvgDocument LoadSvg(Stream sourceStream)
        {
            try
            {
                return SvgDocument.Open<SvgDocument>(sourceStream);
            }
            catch
            {
                throw new RequestInputException(callContext.Localized.Get("SvgImageCanNotBeLoaded"));
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
        public StoreImage(CallContext callContext)
        {
            this.callContext = callContext;
        }
        public async Task RunAsync(Request request, DateTime? now = null)
        {
            await request.CheckValidityAsync(callContext.DbContext, callContext.Localized);

            var owner = await callContext.DbContext.Users.SingleAsync(u => u.Id == request.Owner);

            var image = new Domain.Image
            {
                Name = request.Name,
                Description = request.Description,
                Source = request.Source,
                Owner = owner,
                VersionDescription = callContext.Localized.Get("InitialImageVersionCreation"),
                VersionType = ImageVersionType.Creation,
                InitialUploadUtcDate = now ?? DateTime.UtcNow
            };
            image.LastChangeUtcDate = image.InitialUploadUtcDate;

            image.OriginalContentType = request.ContentType;
            image.OriginalSize = request.Blob.Length;
            image.OriginalBlob = request.Blob;

            using var sourceStream = new MemoryStream(request.Blob);
            using var originalImage = GetBitmap(sourceStream, request.ContentType);
            image.SmallBlob = ResizeImage(originalImage, 100);
            image.SmallBlobSize = image.SmallBlob.Length;
            image.MediumBlob = ResizeImage(originalImage, 600);
            image.MediumBlobSize = image.MediumBlob.Length;
            image.BigBlob = ResizeImage(originalImage, bigImageWidth);
            image.BigBlobSize = image.BigBlob.Length;
            callContext.DbContext.Images.Add(image);
            await callContext.DbContext.SaveChangesAsync();

            callContext.TelemetryClient.TrackEvent("StoreImage",
                ("ImageName", request.Name.ToString()),
                ("DescriptionLength", request.Description.Length.ToString()),
                ("SourceFieldLength", request.Source.Length.ToString()),
                ("ContentType", request.ContentType),
                ("ImageSize", image.OriginalSize.ToString())
                );
        }
        #region Request class
        public sealed class Request
        {
            #region Fields
            public const int minBlobLength = 10;
            public const int maxBlobLength = 10000000;
            #endregion
            public Request(Guid owner, string name, string description, string source, string contentType, IEnumerable<byte> blob)
            {
                Name = name;
                Description = description;
                Source = source;
                ContentType = contentType;
                Owner = owner;
                Blob = blob.ToArray();
            }
            public Guid Owner { get; }
            public string Name { get; }
            public string Description { get; }
            public string Source { get; }
            public string ContentType { get; }
            public byte[] Blob { get; }
            public async Task CheckValidityAsync(MemCheckDbContext dbContext, ILocalized localizer)
            {
                //ImageMetadataInputValidator.Run(this, localizer);
                if (!supportedContentTypes.Contains(ContentType))
                    throw new InvalidOperationException(localizer.Get("InvalidImageContentType") + $" '{ContentType}'");
                if (Blob.Length < minBlobLength || Blob.Length > maxBlobLength)
                    throw new RequestInputException(localizer.Get("InvalidBlobLength") + $" {Blob.Length}" + localizer.Get("MustBeBetween") + $" {minBlobLength} " + localizer.Get("And") + $" {maxBlobLength}");

                await QueryValidationHelper.CheckCanCreateImageWithNameAsync(Name, dbContext, localizer);
                QueryValidationHelper.CheckCanCreateImageWithDescription(Description, localizer);
                QueryValidationHelper.CheckCanCreateImageWithSource(Source, localizer);
            }
        }
        #endregion
    }
}
