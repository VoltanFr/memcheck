using System;
using System.ComponentModel.DataAnnotations;

namespace MemCheck.Domain;

public sealed class CardDiscussionDeletedEntry
{
    [Key] public Guid Id { get; set; }  //id of the CardDiscussionDeletedEntry, not of the card
    public Guid Card { get; set; }
    public MemCheckUser DeletetionAuthor { get; set; } = null!;
    public DateTime DeletetionUtcDate { get; set; } //In case of deletion, this is the deletion date
    public CardDiscussionEntryPreviousVersion PreviousVersion { get; set; } = null!;  //can not be null, since there is at least one version which was deleted
}
