using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations.Schema;

namespace MemCheck.Domain;

public enum TagVersionType { Creation, Changes }

//Deleting a tag is not possible, because a tag may be used in an old version of a card. If we were to allow that, we should implement some sort of soft deletion
public sealed class Tag
{
    public const int MinNameLength = 3;
    public const int MaxNameLength = 50;
    public const int MaxDescriptionLength = 5000;
    public const string Perso = "Perso";

    [Key] public Guid Id { get; set; }
    public MemCheckUser CreatingUser { get; set; } = null!;
    public DateTime VersionUtcDate { get; set; }
    [StringLength(MaxNameLength, MinimumLength = MinNameLength)] public string Name { get; set; } = null!;
    [StringLength(MaxDescriptionLength)] public string Description { get; set; } = null!;
    public IList<TagInCard> TagsInCards { get; set; } = null!;
    public int CountOfPublicCards { get; set; }
    public double AverageRatingOfPublicCards { get; set; }
    public string VersionDescription { get; set; } = null!;
    [Column(TypeName = "int")] public TagVersionType VersionType { get; set; }
    public TagPreviousVersion? PreviousVersion { get; set; }  //null for initial version (VersionType == Creation)

    public override bool Equals(object? obj)
    {
        return obj != null && obj is Tag tag && Id == tag.Id;
    }
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}

public sealed class TagPreviousVersion
{
    [Key] public Guid Id { get; set; }
    public Guid Tag { get; set; }
    public MemCheckUser CreatingUser { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string VersionDescription { get; set; } = null!;
    [Column(TypeName = "int")] public TagVersionType VersionType { get; set; }
    public TagPreviousVersion? PreviousVersion { get; set; }  //null for initial version (VersionType == Creation)
}
