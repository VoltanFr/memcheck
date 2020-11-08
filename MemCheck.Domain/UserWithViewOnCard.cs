using System;
using System.ComponentModel.DataAnnotations;

namespace MemCheck.Domain
{
    public sealed class UserWithViewOnCard
    {
        [Key] public Guid UserId { get; set; }
        public MemCheckUser User { get; set; } = null!;

        [Key] public Guid CardId { get; set; }
        public Card Card { get; set; } = null!;
    }

}
