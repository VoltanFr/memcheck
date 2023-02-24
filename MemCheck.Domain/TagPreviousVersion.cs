using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations.Schema;

namespace MemCheck.Domain;

public sealed class TagPreviousVersion
{
    [Key] public Guid Id { get; set; }
    public Guid Tag { get; set; }
    public MemCheckUser CreatingUser { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string VersionDescription { get; set; } = null!;
    [Column(TypeName = "int")] public TagVersionType VersionType { get; set; }
    public DateTime VersionUtcDate { get; set; }
    public TagPreviousVersion? PreviousVersion { get; set; }  //null for initial version (VersionType == Creation)
}
