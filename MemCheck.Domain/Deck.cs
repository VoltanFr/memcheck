using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MemCheck.Domain;

public sealed class Deck
{
    [NotMapped] public const int UnknownDeckId = 0;
    [NotMapped] public const int DefaultHeapingAlgorithmId = 1;

    [Key] public Guid Id { get; set; }
    [Key] public MemCheckUser Owner { get; set; } = null!;
    public string Description { get; set; } = "";
    public IList<CardInDeck> CardInDecks { get; set; } = null!;
    public int HeapingAlgorithmId { get; set; } = DefaultHeapingAlgorithmId;
}
