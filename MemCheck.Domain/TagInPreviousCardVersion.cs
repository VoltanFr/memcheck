using System;
using System.ComponentModel.DataAnnotations;

namespace MemCheck.Domain;

public sealed class TagInPreviousCardVersion
{
    [Key] public Guid TagId { get; set; }
    public Tag Tag { get; set; } = null!;

    [Key] public Guid CardPreviousVersionId { get; set; }
    public CardPreviousVersion CardPreviousVersion { get; set; } = null!;
}

