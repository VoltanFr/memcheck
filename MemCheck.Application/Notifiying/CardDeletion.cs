using MemCheck.Basics;
using System;

namespace MemCheck.Application.Notifiying
{
    public sealed class CardDeletion
    {
        public CardDeletion(string frontSide, string deletionAuthor, DateTime deletionUtcDate, string deletionDescription, bool cardIsViewable)
        {
            FrontSide = cardIsViewable ? frontSide.Truncate(Notifier.MaxLengthForTextFields) : null;
            DeletionAuthor = deletionAuthor;
            DeletionUtcDate = deletionUtcDate;
            DeletionDescription = cardIsViewable ? deletionDescription.Truncate(Notifier.MaxLengthForTextFields) : null;
            CardIsViewable = cardIsViewable;
        }
        public string? FrontSide { get; }
        public string DeletionAuthor { get; }
        public DateTime DeletionUtcDate { get; }
        public string? DeletionDescription { get; }
        public bool CardIsViewable { get; }
    }
}