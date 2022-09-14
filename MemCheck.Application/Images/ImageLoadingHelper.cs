using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;

namespace MemCheck.Application.Images;

internal static class ImageLoadingHelper
{
    private const string imageNameGroup = "imageName";
    private const string imageRegExPattern = @$"(!\[Mnesios:(?<{imageNameGroup}>{QueryValidationHelper.charsAllowedInImageName}+)((,size=small)|(,size=medium)|(,size=big))?\])|(`([^`]+)?`)";
    public static ImmutableDictionary<Guid, ImageDetails> GetAllImageNames(MemCheckDbContext dbContext)
    {
        return dbContext.Images.AsNoTracking()
            .Select(i => new ImageDetails(i.Id, i.Name, i.Description, i.Owner.UserName, i.Source, i.InitialUploadUtcDate, i.LastChangeUtcDate, i.VersionDescription, i.OriginalContentType, i.OriginalSize, i.SmallBlobSize, i.MediumBlobSize, i.BigBlobSize))
            .ToImmutableDictionary(i => i.Id, i => i);
    }
    public static ImmutableHashSet<string> GetMnesiosImagesFromText(string text)
    {
        var regEx = new Regex(imageRegExPattern);

        var matches = regEx.Matches(text);
        var result = new List<string>();

        foreach (var match in matches.Cast<Match>())
            if (match.Success)
            {
                var imageName = match.Groups[imageNameGroup];
                if (imageName != null && !string.IsNullOrEmpty(imageName.Value))
                    result.Add(imageName.Value);
            }

        return result.ToImmutableHashSet();
    }
    public static ImmutableHashSet<string> GetMnesiosImagesFromSides(string frontSide, string backSide, string additionalInfo)
    {
        var result = new List<string>();
        result.AddRange(GetMnesiosImagesFromText(frontSide));
        result.AddRange(GetMnesiosImagesFromText(backSide));
        result.AddRange(GetMnesiosImagesFromText(additionalInfo));
        return result.ToImmutableHashSet();
    }
}
