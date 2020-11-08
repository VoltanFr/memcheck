using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MemCheck.Domain
{
    public enum CardPreviousVersionType { Creation, Changes, Deletion }

    public sealed class CardPreviousVersion
    {
        [Key] public Guid Id { get; set; }  //id of the CardPreviousVersion, not of the card
        public Guid Card { get; set; }
        public MemCheckUser VersionCreator { get; set; } = null!;
        public CardLanguage CardLanguage { get; set; } = null!;
        public string FrontSide { get; set; } = null!;
        public string BackSide { get; set; } = null!;
        public string AdditionalInfo { get; set; } = null!;
        public IEnumerable<TagInPreviousCardVersion> Tags { get; set; } = null!;
        public DateTime VersionUtcDate { get; set; }
        public IEnumerable<UserWithViewOnCardPreviousVersion> UsersWithView { get; set; } = null!; //Empty list means public ; Only owner in list means private ; Some users in list means visible for them
        public IEnumerable<ImageInCardPreviousVersion> Images { get; set; } = null!;
        [Column(TypeName = "int")] public CardPreviousVersionType VersionType { get; set; }
        public string VersionDescription { get; set; } = null!;
        public CardPreviousVersion? PreviousVersion { get; set; }  //null for initial version (VersionType == Creation)
    }
}
