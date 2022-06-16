using System;

namespace MemCheck.Application.Images;

public sealed record ImageDetails(Guid Id, string Name, string Description, string UploaderUserName, string Source, DateTime InitialUploadUtcDate, DateTime LastChangeUtcDate, string VersionDescription, int CardCount, string OriginalImageContentType, int OriginalImageSize, int SmallSize, int MediumSize, int BigSize);
