using MemCheck.Application.QueryValidation;
using MemCheck.Basics;
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

namespace MemCheck.Application.Windows.Images;

public sealed class StoreImage : RequestRunner<StoreImage.Request, StoreImage.Result>
{
    #region Fields
    public const string svgImageContentType = "image/svg+xml";
    public const string pngImageContentType = "image/png";
    private static readonly ImmutableHashSet<string> supportedContentTypes = GetSupportedContentTypes();
    private readonly DateTime? runDate;
    private const int bigImageWidth = 1600;
    #endregion
    #region Private methods
    private static ImmutableHashSet<string> GetSupportedContentTypes()
    {
        return new HashSet<string>(new[] { svgImageContentType, "image/jpeg", pngImageContentType, "image/gif" }).ToImmutableHashSet();
    }
    public static byte[] ResizeImage(Bitmap originalImage, int targetWidth)
    {
        var targetheight = originalImage.Height * targetWidth / originalImage.Width;
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
            throw new RequestInputException(Localized.GetLocalized("SvgImageCanNotBeLoaded"));
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
    public StoreImage(CallContext callContext, DateTime? runDate = null) : base(callContext)
    {
        this.runDate = runDate;
    }
    protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
    {
        var owner = await DbContext.Users.SingleAsync(u => u.Id == request.Owner);

        var image = new Domain.Image
        {
            Name = request.Name,
            Description = request.Description,
            Source = request.Source,
            Owner = owner,
            VersionDescription = Localized.GetLocalized("InitialImageVersionCreation"),
            VersionType = ImageVersionType.Creation,
            InitialUploadUtcDate = runDate ?? DateTime.UtcNow
        };
        image.LastChangeUtcDate = image.InitialUploadUtcDate;

        image.OriginalContentType = request.ContentType;
        var blob = request.Blob.ToArray();
        image.OriginalSize = blob.Length;
        image.OriginalBlob = blob;
        image.OriginalBlobSha1 = CryptoServices.GetSHA1(blob);

        using var sourceStream = new MemoryStream(blob);
        using var originalImage = GetBitmap(sourceStream, request.ContentType);
        image.SmallBlob = ResizeImage(originalImage, 100);
        image.SmallBlobSize = image.SmallBlob.Length;
        image.MediumBlob = ResizeImage(originalImage, 600);
        image.MediumBlobSize = image.MediumBlob.Length;
        image.BigBlob = ResizeImage(originalImage, bigImageWidth);
        image.BigBlobSize = image.BigBlob.Length;
        DbContext.Images.Add(image);
        await DbContext.SaveChangesAsync();

        return new ResultWithMetrologyProperties<Result>(new Result(image.Id),
            ("ImageName", request.Name.ToString()),
            IntMetric("DescriptionLength", request.Description.Length),
            IntMetric("SourceFieldLength", request.Source.Length),
            ("ContentType", request.ContentType),
            IntMetric("ImageSize", image.OriginalSize));
    }
    #region Request & Result
    public sealed class Request : IRequest
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
            Blob = blob;
        }
        public Guid Owner { get; }
        public string Name { get; }
        public string Description { get; }
        public string Source { get; }
        public string ContentType { get; }
        public IEnumerable<byte> Blob { get; }
        public async Task CheckValidityAsync(CallContext callContext)
        {
            //ImageMetadataInputValidator.Run(this, localizer);
            if (!supportedContentTypes.Contains(ContentType))
                throw new InvalidOperationException(callContext.Localized.GetLocalized("InvalidImageContentType") + $" '{ContentType}'");
            if (Blob.Count() is < minBlobLength or > maxBlobLength)
                throw new RequestInputException(callContext.Localized.GetLocalized("InvalidBlobLength") + $" {Blob.Count()}" + callContext.Localized.GetLocalized("MustBeBetween") + $" {minBlobLength} " + callContext.Localized.GetLocalized("And") + $" {maxBlobLength}");

            await QueryValidationHelper.CheckCanCreateImageWithNameAsync(Name, callContext.DbContext, callContext.Localized);
            QueryValidationHelper.CheckCanCreateImageWithDescription(Description, callContext.Localized);
            QueryValidationHelper.CheckCanCreateImageWithSource(Source, callContext.Localized);

            var sha1 = CryptoServices.GetSHA1(Blob.ToArray());
            var existing = await callContext.DbContext.Images.Where(img => img.OriginalBlobSha1.SequenceEqual(sha1)).SingleOrDefaultAsync();
            if (existing != null)
                throw new IOException($"This image is already in the database, with name '{existing.Name}'");
        }
    }
    public sealed record Result(Guid ImageId);
    #endregion
}
