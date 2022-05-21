using System;
using System.ComponentModel.DataAnnotations;

namespace MemCheck.Domain;

public sealed class CardLanguage
{
    [Key] public Guid Id { get; set; }
    [StringLength(50, MinimumLength = 3)] public string Name { get; set; } = null!;
}
