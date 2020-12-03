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
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application
{
    public sealed class UpdateImageMetadata
    {
        #region Fields
        private const string svgImageContentType = "image/svg+xml";
        private readonly MemCheckDbContext dbContext;
        private readonly IStringLocalizer localizer;
        private const int minDescriptionLength = 3;
        private const int maxDescriptionLength = 1000;
        #endregion
        #region Private methods
        private static ImmutableHashSet<string> GetSupportedContentTypes()
        {
            return new HashSet<string>(new string[] { svgImageContentType, "image/jpeg", "image/png" }).ToImmutableHashSet();
        }
        public byte[] ResizeImage(Bitmap originalImage, int targetWidth)
        {
            int targetheight = originalImage.Height * targetWidth / originalImage.Width;
            using (var resultImage = new Bitmap(targetWidth, targetheight))
            using (var resultImageGraphics = Graphics.FromImage(resultImage))
            {
                resultImageGraphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
                resultImageGraphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                resultImageGraphics.Clear(Color.White);
                resultImageGraphics.DrawImage(originalImage, new Rectangle(0, 0, targetWidth, targetheight), new Rectangle(0, 0, originalImage.Width, originalImage.Height), GraphicsUnit.Pixel);
                using (var targetStream = new MemoryStream())
                {
                    resultImage.Save(targetStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                    return targetStream.ToArray();
                }
            }
        }
        private Bitmap GetBitmap(Stream sourceStream, string contentType)
        {
            if (contentType.Equals(svgImageContentType, StringComparison.OrdinalIgnoreCase))
            {
                var svgDocument = SvgDocument.Open<SvgDocument>(sourceStream);
                return svgDocument.Draw();
            }
            return new Bitmap(sourceStream);
        }
        private ImagePreviousVersionType ImagePreviousVersionTypeFromImage(Domain.Image i)
        {
            switch (i.VersionType)
            {
                case ImageVersionType.Creation:
                    return ImagePreviousVersionType.Creation;
                case ImageVersionType.Changes:
                    return ImagePreviousVersionType.Changes;
                default:
                    throw new NotImplementedException();
            }
        }
        #endregion
        public UpdateImageMetadata(MemCheckDbContext dbContext, IStringLocalizer localizer)
        {
            this.dbContext = dbContext;
            this.localizer = localizer;
        }
        public async Task RunAsync(Request request)
        {
            await request.CheckValidityAsync(localizer, dbContext);
            var image = await dbContext.Images.Include(img => img.PreviousVersion).SingleAsync(img => img.Id == request.ImageId);

            var versionFromCurrentImage = new ImagePreviousVersion()
            {
                Image = request.ImageId,
                Owner = image.Owner,
                Name = image.Name,
                Description = image.Description,
                Source = image.Source,
                InitialUploadUtcDate = image.InitialUploadUtcDate,
                VersionUtcDate = image.LastChangeUtcDate,
                OriginalContentType = image.OriginalContentType,
                OriginalSize = image.OriginalSize,
                OriginalBlob = image.OriginalBlob,
                VersionType = ImagePreviousVersionTypeFromImage(image),
                VersionDescription = image.VersionDescription,
                PreviousVersion = image.PreviousVersion,
            };

            dbContext.ImagePreviousVersions.Add(versionFromCurrentImage);

            image.Owner = request.User;
            image.Name = request.Name;
            image.Description = request.Description;
            image.Source = request.Source;
            image.LastChangeUtcDate = DateTime.UtcNow;
            image.VersionType = ImageVersionType.Changes;
            image.VersionDescription = request.VersionDescription;
            image.PreviousVersion = versionFromCurrentImage;

            await dbContext.SaveChangesAsync();
        }
        #region Request class
        public sealed class Request : IImageMetadata
        {
            public Request(Guid imageId, MemCheckUser user, string name, string source, string description, string versionDescription)
            {
                ImageId = imageId;
                User = user;
                VersionDescription = versionDescription;
                Name = name.Trim();
                Source = source.Trim();
                Description = description.Trim();
            }
            public Guid ImageId { get; }
            public MemCheckUser User { get; }
            public string Name { get; }
            public string Source { get; }
            public string Description { get; }
            public string VersionDescription { get; }
            public async Task CheckValidityAsync(IStringLocalizer localizer, MemCheckDbContext dbContext)
            {
                if (QueryValidationHelper.IsReservedGuid(User.Id))
                    throw new RequestInputException("Invalid user id");
                if (QueryValidationHelper.IsReservedGuid(ImageId))
                    throw new RequestInputException("Invalid image id");

                ImageMetadataInputValidator.Run(this, localizer);

                if (VersionDescription != VersionDescription.Trim())
                    throw new InvalidOperationException("Invalid VersionDescription: not trimmed");
                if (VersionDescription.Length < minDescriptionLength || VersionDescription.Length > maxDescriptionLength)
                    throw new RequestInputException(localizer["InvalidVersionDescriptionLength"].Value + $" {VersionDescription.Length}" + localizer["MustBeBetween"].Value + $" {minDescriptionLength} " + localizer["And"] + $" {maxDescriptionLength}");

                var images = dbContext.Images.Where(img => img.Id == ImageId);

                if (!await images.AnyAsync())
                    throw new RequestInputException("Unknown image id");

                var imageDataBeforeUpdate = await images.Select(img => new { nameBeforeUpdate = img.Name, sourceBeforeUpdate = img.Source, descriptionBeforeUpdate = img.Description }).SingleAsync();

                if ((imageDataBeforeUpdate.nameBeforeUpdate == Name) && (imageDataBeforeUpdate.sourceBeforeUpdate == Source) && (imageDataBeforeUpdate.descriptionBeforeUpdate == Description))
                    throw new RequestInputException(localizer["CanNotUpdateMetadataBecauseSameAsOriginal"].Value);
            }
        }
        #endregion
    }
}
