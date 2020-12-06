using System;

namespace MemCheck.Application.Notifying
{
    public sealed class RegisteredCardDeletion
    {
        public RegisteredCardDeletion(string frontSide, string deletionAuthor, DateTime deletionUtcDate, string deletionDescription, bool cardIsViewable)
        {
            FrontSide = cardIsViewable ? frontSide.Truncate(Notifier.MaxLengthForTextFields, true) : null;
            DeletionAuthor = deletionAuthor;
            DeletionUtcDate = deletionUtcDate;
            DeletionDescription = cardIsViewable ? deletionDescription.Truncate(Notifier.MaxLengthForTextFields, true) : null;
            CardIsViewable = cardIsViewable;
        }
        public string? FrontSide { get; }
        public string DeletionAuthor { get; }
        public DateTime DeletionUtcDate { get; }
        public string? DeletionDescription { get; }
        public bool CardIsViewable { get; }
    }
}