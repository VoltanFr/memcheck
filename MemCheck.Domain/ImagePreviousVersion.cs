using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MemCheck.Domain
{
    public enum ImagePreviousVersionType { Creation, Changes, Deletion }

    public sealed class ImagePreviousVersion
    {
        [Key] public Guid Id { get; set; }
        public Guid Image { get; set; }
        public MemCheckUser Owner { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Source { get; set; } = null!;  //eg copyright
        public DateTime InitialUploadUtcDate { get; set; }  //This field is immutable accross versions, we should remove it: having it in the Image class is usefuly for perf
        public DateTime VersionUtcDate { get; set; }
        public string OriginalContentType { get; set; } = null!; //eg "image/svg+xml" or "image/jpeg"
        public int OriginalSize { get; set; }   //In bytes
        public byte[] OriginalBlob { get; set; } = null!;
        [Column(TypeName = "int")] public ImagePreviousVersionType VersionType { get; set; }
        public string VersionDescription { get; set; } = null!;
        public ImagePreviousVersion? PreviousVersion { get; set; }  //null for initial version (VersionType == Creation)
    }
}
