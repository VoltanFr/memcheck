using System;
using System.ComponentModel.DataAnnotations;

namespace MemCheck.Domain;

public sealed class CardDiscussionEntry
{
    [Key] public Guid Id { get; set; }  //id of the CardDiscussionEntry, not of the card
    public Guid Card { get; set; }
    public MemCheckUser Creator { get; set; } = null!;
    public string Text { get; set; } = null!;
    public DateTime CreationUtcDate { get; set; }
    public CardDiscussionEntryPreviousVersion? PreviousVersion { get; set; }  //null for initial version, when there was no edit
    public CardDiscussionEntry? PreviousEntry { get; set; }  //null for initial version
}
