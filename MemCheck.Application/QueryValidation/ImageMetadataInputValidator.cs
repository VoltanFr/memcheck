using System;

namespace MemCheck.Application.QueryValidation
{
    internal static class ImageMetadataInputValidator
    {
        #region Fields
        private const int minNameLength = 3;
        private const int maxNameLength = 100;
        private const int minDescriptionLength = 3;
        private const int maxDescriptionLength = 5000;
        private const int minSourceLength = 3;
        private const int maxSourceLength = 1000;
        #endregion
        public static void Run(IImageMetadata input, ILocalized localizer)
        {
            if (input.Name != input.Name.Trim())
                throw new InvalidOperationException("Invalid name: not trimmed");
            if (input.Name.Length < minNameLength || input.Name.Length > maxNameLength)
                throw new RequestInputException(localizer.Get("InvalidNameLength") + $" {input.Name.Length}" + localizer.Get("MustBeBetween") + $" {minNameLength} " + localizer.Get("And") + $" {maxNameLength}");

            if (input.Description != input.Description.Trim())
                throw new InvalidOperationException("Invalid description: not trimmed");
            if (input.Description.Length < minDescriptionLength || input.Description.Length > maxDescriptionLength)
                throw new RequestInputException(localizer.Get("InvalidDescriptionLength") + $" {input.Description.Length}" + localizer.Get("MustBeBetween") + $" {minDescriptionLength} " + localizer.Get("And") + $" {maxDescriptionLength}");

            if (input.Source != input.Source.Trim())
                throw new InvalidOperationException("Invalid source: not trimmed");
            if (input.Source.Length < minSourceLength || input.Source.Length > maxSourceLength)
                throw new RequestInputException(localizer.Get("InvalidSourceLength") + $" {input.Source.Length}" + localizer.Get("MustBeBetween") + $" {minSourceLength} " + localizer.Get("And") + $" {maxSourceLength}");
        }
    }

}
