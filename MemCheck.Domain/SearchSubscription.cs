using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MemCheck.Domain
{
    public sealed class SearchSubscription
    {
        [Key] public Guid SearchId { get; set; }
        public Guid UserId { get; set; }
        public Guid ExcludedDeck { get; set; } //Guid.Empty means ignore
        public string RequiredText { get; set; } = null!;
        public IEnumerable<RequiredTagInSearchSubscription> RequiredTags { get; set; } = null!;
        public bool excludeAllTags { get; set; }
        public IEnumerable<ExcludedTagInSearchSubscription> ExcludedTags { get; set; } = null!;

        public DateTime RegistrationUtcDate { get; set; }
        public DateTime LastNotificationUtcDate { get; set; }
    }

    public sealed class RequiredTagInSearchSubscription
    {
        [Key] public Guid SearchSubscriptionId { get; set; }
        [Key] public Guid TagId { get; set; }
    }

    public sealed class ExcludedTagInSearchSubscription
    {
        [Key] public Guid SearchSubscriptionId { get; set; }
        [Key] public Guid TagId { get; set; }
    }
}
