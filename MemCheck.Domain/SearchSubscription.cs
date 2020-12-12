using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MemCheck.Domain
{
    public sealed class SearchSubscription
    {
        [Key] public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public Guid UserId { get; set; }
        public Guid ExcludedDeck { get; set; } //Guid.Empty means ignore
        public string RequiredText { get; set; } = null!;
        public IEnumerable<RequiredTagInSearchSubscription> RequiredTags { get; set; } = null!;
        public bool ExcludeAllTags { get; set; }
        public IEnumerable<ExcludedTagInSearchSubscription> ExcludedTags { get; set; } = null!;
        public DateTime RegistrationUtcDate { get; set; }
        public DateTime LastRunUtcDate { get; set; }
        public IEnumerable<CardInSearchResult> CardsInLastRun { get; set; } = null!; //Complete set of cards reported by the search (not the cards reported to the user)
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

    public sealed class CardInSearchResult
    {
        [Key] public Guid SearchSubscriptionId { get; set; }
        [Key] public Guid CardId { get; set; }
    }
}
