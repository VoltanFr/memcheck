using System;
using System.ComponentModel.DataAnnotations;

namespace MemCheck.Domain;

public sealed class UserWithViewOnCardPreviousVersion
{
    [Key] public Guid AllowedUserId { get; set; }
    public MemCheckUser AllowedUser { get; set; } = null!;

    [Key] public Guid CardPreviousVersionId { get; set; }
    public CardPreviousVersion CardPreviousVersion { get; set; } = null!;
}

