using System;

namespace MemCheck.Domain
{
    public sealed class UserCardRating
    {
        public Guid CardId { get; set; }
        public Card Card { get; set; } = null!;

        public Guid UserId { get; set; }
        public MemCheckUser User { get; set; } = null!;

        public int Rating { get; set; } //A rating is between 1 and 5
    }
}
