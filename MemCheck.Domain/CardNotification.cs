using System;
using System.ComponentModel.DataAnnotations;

namespace MemCheck.Domain
{
    public sealed class CardNotification
    {
        public const int CardNotificationRegistrationMethod_AddToDeck = 1;
        public const int CardNotificationRegistrationMethod_ExplicitByUser = 2; //Eg in card edit page
        public const int CardNotificationRegistrationMethod_VersionCreation = 3;

        public Guid CardId { get; set; }
        public Card Card { get; set; } = null!;

        public Guid UserId { get; set; }
        public MemCheckUser User { get; set; } = null!;

        public DateTime RegistrationUtcDate { get; set; }
        public int RegistrationMethod { get; set; } //One of the constants above
        public DateTime LastNotificationUtcDate { get; set; }
    }
}
