using System;
using System.ComponentModel.DataAnnotations;

namespace MemCheck.Domain
{
    public sealed class UserWithViewOnCardPreviousVersion
    {
        [Key] public Guid AllowedUserId { get; set; }

        [Key] public Guid CardPreviousVersionId { get; set; }
        public CardPreviousVersion CardPreviousVersion { get; set; } = null!;
    }

}
