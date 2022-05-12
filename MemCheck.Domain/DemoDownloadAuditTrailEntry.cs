using System;
using System.ComponentModel.DataAnnotations;

namespace MemCheck.Domain
{
    //We have no way to know that the same user uses the demo mode multiple times, since we have no way to identify the user, or even the client process or machine (keeping the IP address would not obey GDPR rules)
    public sealed class DemoDownloadAuditTrailEntry
    {
        [Key] public Guid Id { get; set; }
        public Guid TagId { get; set; }
        public DateTime DownloadUtcDate { get; set; }
        public int CountOfCardsReturned { get; set; }
    }
}
