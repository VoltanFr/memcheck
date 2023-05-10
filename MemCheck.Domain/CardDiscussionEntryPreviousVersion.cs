using System;
using System.ComponentModel.DataAnnotations;

namespace MemCheck.Domain;

public sealed class CardDiscussionEntryPreviousVersion
{
    [Key] public Guid Id { get; set; }  //id of the CardDiscussionEntryPreviousVersion, not of the card
    public Guid Card { get; set; }
    public MemCheckUser Creator { get; set; } = null!;
    public string Text { get; set; } = null!;
    public DateTime CreationUtcDate { get; set; }
    public CardDiscussionEntryPreviousVersion? PreviousVersion { get; set; }  //null for initial version
}
