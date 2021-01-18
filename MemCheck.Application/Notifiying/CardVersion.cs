using System;

namespace MemCheck.Application.Notifying
{
    public class CardVersion
    {
        public CardVersion(Guid cardId, string frontSide, string versionCreator, DateTime versionUtcDate, string versionDescription, bool cardIsViewable, Guid? versionIdOnLastNotification)
        {
            CardId = cardId;
            FrontSide = cardIsViewable ? frontSide.Truncate(Notifier.MaxLengthForTextFields) : null;
            VersionCreator = versionCreator;
            VersionUtcDate = versionUtcDate;
            VersionDescription = cardIsViewable ? versionDescription.Truncate(Notifier.MaxLengthForTextFields) : null;
            CardIsViewable = cardIsViewable;
            VersionIdOnLastNotification = versionIdOnLastNotification;
        }
        public Guid CardId { get; }
        public string? FrontSide { get; }
        public string VersionCreator { get; }
        public DateTime VersionUtcDate { get; }
        public string? VersionDescription { get; }
        public bool CardIsViewable { get; }
        public Guid? VersionIdOnLastNotification { get; }
    }
}