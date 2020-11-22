using System;

namespace MemCheck.Domain
{
    public sealed class CardNotificationSubscription
    {
        public const int CardNotificationRegistrationMethod_AddToDeck = 1;
        public const int CardNotificationRegistrationMethod_ExplicitByUser = 2; //Eg in card edit page
        public const int CardNotificationRegistrationMethod_VersionCreation = 3;

        public Guid CardId { get; set; }    //Note that we can not have a navigational property to the card, because it would not work for a deleted card (we don't want to lose the notif even after deletion), and a navigational property brings a cascade delete strategy

        public Guid UserId { get; set; }
        public MemCheckUser User { get; set; } = null!;

        public DateTime RegistrationUtcDate { get; set; }
        public int RegistrationMethod { get; set; } //One of the constants above
        public DateTime LastNotificationUtcDate { get; set; }
    }
}
