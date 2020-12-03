using Microsoft.Extensions.Localization;
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
        public static void Run(IImageMetadata input, IStringLocalizer localizer)
        {
            if (input.Name != input.Name.Trim())
                throw new InvalidOperationException("Invalid name: not trimmed");
            if (input.Name.Length < minNameLength || input.Name.Length > maxNameLength)
                throw new RequestInputException(localizer["InvalidNameLength"] + $" {input.Name.Length}" + localizer["MustBeBetween"] + $" {minNameLength} " + localizer["And"] + $" {maxNameLength}");

            if (input.Description != input.Description.Trim())
                throw new InvalidOperationException("Invalid description: not trimmed");
            if (input.Description.Length < minDescriptionLength || input.Description.Length > maxDescriptionLength)
                throw new RequestInputException(localizer["InvalidDescriptionLength"] + $" {input.Description.Length}" + localizer["MustBeBetween"] + $" {minDescriptionLength} " + localizer["And"] + $" {maxDescriptionLength}");

            if (input.Source != input.Source.Trim())
                throw new InvalidOperationException("Invalid source: not trimmed");
            if (input.Source.Length < minSourceLength || input.Source.Length > maxSourceLength)
                throw new RequestInputException(localizer["InvalidSourceLength"] + $" {input.Source.Length}" + localizer["MustBeBetween"] + $" {minSourceLength} " + localizer["And"] + $" {maxSourceLength}");
        }
    }

}
