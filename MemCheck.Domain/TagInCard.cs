using System;
using System.ComponentModel.DataAnnotations;

namespace MemCheck.Domain
{
    public sealed class TagInCard
    {
        [Key] public Guid TagId { get; set; }
        public Tag Tag { get; set; } = null!;

        [Key] public Guid CardId { get; set; }
        public Card Card { get; set; } = null!;

        public override bool Equals(object? obj)
        {
            // If the passed object is null
            if (obj == null)
            {
                return false;
            }
            if (!(obj is TagInCard))
            {
                return false;
            }
            return TagId == ((TagInCard)obj).TagId;
        }
        public override int GetHashCode()
        {
            return TagId.GetHashCode();
        }
    }
}
