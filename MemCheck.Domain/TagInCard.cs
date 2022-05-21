using System;
using System.ComponentModel.DataAnnotations;

namespace MemCheck.Domain;

public sealed class TagInCard
{
    [Key] public Guid TagId { get; set; }
    public Tag Tag { get; set; } = null!;

    [Key] public Guid CardId { get; set; }
    public Card Card { get; set; } = null!;

    public override bool Equals(object? obj)
    {
        return obj != null && obj is TagInCard card && TagId == card.TagId;
    }
    public override int GetHashCode()
    {
        return TagId.GetHashCode();
    }
}
