using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MemCheck.Domain;

public enum ImageVersionType { Creation, Changes }

/* Image names are unique, and the regexp of allowed chars is defined by the constant `charsAllowedInImageName` in `MarkdownConversion.js`.
 */

public sealed class Image
{
    [Key] public Guid Id { get; set; }
    public MemCheckUser Owner { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Source { get; set; } = null!;  //eg copyright
    public DateTime InitialUploadUtcDate { get; set; }
    public DateTime LastChangeUtcDate { get; set; }
    public string VersionDescription { get; set; } = null!;
    [Column(TypeName = "int")] public ImageVersionType VersionType { get; set; }

    public string OriginalContentType { get; set; } = null!; //eg "image/svg+xml" or "image/jpeg". But the three blobs below are always "image/jpeg"
    public int OriginalSize { get; set; }   //In bytes
    public byte[] OriginalBlob { get; set; } = null!;
    public byte[] OriginalBlobSha1 { get; set; } = null!;

    public byte[] SmallBlob { get; set; } = null!;    //Width=100px
    public int SmallBlobSize { get; set; }
    public byte[] MediumBlob { get; set; } = null!;    //Width=600px
    public int MediumBlobSize { get; set; }
    public byte[] BigBlob { get; set; } = null!;    //Width=1600px
    public int BigBlobSize { get; set; }

    public ImagePreviousVersion? PreviousVersion { get; set; }  //null for initial version (VersionType == Creation)
    public IEnumerable<ImageInCard> ImagesInCards { get; set; } = null!;
}
