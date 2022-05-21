using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace MemCheck.Domain;

//An account deletion will replace all the fields of the user with some information (eg UserName becomes <deleted user>, same for email, etc.)
//But the user id always remains valid
public sealed class MemCheckUser : IdentityUser<Guid>
{
    public string? UILanguage { get; set; } = null; //This is the value returned by MemCheckSupportedCultures.IdFromCulture for one of the MemCheckSupportedCultures.All
    public CardLanguage? PreferredCardCreationLanguage { get; set; } = null;
    public int MinimumCountOfDaysBetweenNotifs { get; set; } = 0;   //A number of days. <= 0 means never send any email
    public DateTime LastNotificationUtcDate { get; set; }
    public bool SubscribeToCardOnEdit { get; set; }
    public DateTime RegistrationUtcDate { get; set; }
    public DateTime LastSeenUtcDate { get; set; }
    public DateTime? DeletionDate { get; set; } = null; //UTC
    public IEnumerable<UserCardRating> UserCardRating { get; set; } = null!;
    public IEnumerable<UserWithViewOnCard> UsersWithView { get; set; } = null!; //Empty list means public ; If not empty, version creator has to be in the list (only version creator in list means private)
    public IEnumerable<UserWithViewOnCardPreviousVersion> UsersWithViewOnPreviousVersion { get; set; } = null!; //Empty list means public ; If not empty, version creator has to be in the list (only version creator in list means private)
}
