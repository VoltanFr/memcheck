using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MemCheck.Domain
{
    //Deleting a tag is not possible, because a tag may be used in an old version of a card. If we were to allow that, we should implement some sort of soft deletion
    public sealed class Tag
    {
        public const int MinNameLength = 3;
        public const int MaxNameLength = 50;

        [Key] public Guid Id { get; set; }
        [StringLength(MaxNameLength, MinimumLength = MinNameLength)] public string Name { get; set; } = null!;
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
