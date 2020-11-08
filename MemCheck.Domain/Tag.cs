using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MemCheck.Domain
{
    //Deleting a card is not possible, because a tag may be used in an old version of a card
    //If we think something is needed, implement a soft deletion algorithm
    public sealed class Tag
    {
        [Key] public Guid Id { get; set; }
        [StringLength(50, MinimumLength = 3)] public string Name { get; set; } = null!;
        public IList<TagInCard> TagsInCards { get; set; } = null!;
        public override bool Equals(object? obj)
        {
            // If the passed object is null
            if (obj == null)
            {
                return false;
            }
            if (!(obj is Tag))
            {
                return false;
            }
            return Id == ((Tag)obj).Id;
        }
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
