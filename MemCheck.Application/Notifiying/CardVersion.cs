using System;

namespace MemCheck.Application.Notifying
{
    public class CardVersion
    {
        public CardVersion(Guid cardId, string frontSide, string versionCreator, DateTime versionUtcDate, string versionDescription, bool cardIsViewable)
        {
            CardId = cardId;
            FrontSide = cardIsViewable ? frontSide.Truncate(Notifier.MaxLengthForTextFields, true) : null;
            VersionCreator = versionCreator;
            VersionUtcDate = versionUtcDate;
            VersionDescription = cardIsViewable ? versionDescription.Truncate(Notifier.MaxLengthForTextFields, true) : null;
            CardIsViewable = cardIsViewable;
        }
        public Guid CardId { get; }
        public string? FrontSide { get; }
        public string VersionCreator { get; }
        public DateTime VersionUtcDate { get; }
        public string? VersionDescription { get; }
        public bool CardIsViewable { get; }
    }
}