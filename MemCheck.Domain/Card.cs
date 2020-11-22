using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MemCheck.Domain
{
    public enum CardVersionType { Creation, Changes }

    public sealed class Card
    {
        [Key] public Guid Id { get; set; }
        public MemCheckUser VersionCreator { get; set; } = null!;
        public CardLanguage CardLanguage { get; set; } = null!;
        public string FrontSide { get; set; } = null!;
        public string BackSide { get; set; } = null!;
        public string AdditionalInfo { get; set; } = null!;
        public IEnumerable<CardInDeck> CardInDecks { get; set; } = null!;
        public IEnumerable<TagInCard> TagsInCards { get; set; } = null!;
        public DateTime InitialCreationUtcDate { get; set; } //This field is immutable accross versions, but keeping it avoids the need to walk all the versions to find the initial creation date
        public DateTime VersionUtcDate { get; set; }
        public IEnumerable<UserWithViewOnCard> UsersWithView { get; set; } = null!; //Empty list means public ; If not empty, version creator has to be in the list (only version creator in list means private)
        public IEnumerable<ImageInCard> Images { get; set; } = null!;
        public string VersionDescription { get; set; } = null!;
        [Column(TypeName = "int")] public CardVersionType VersionType { get; set; }
        public CardPreviousVersion? PreviousVersion { get; set; }  //null for initial version (VersionType == Creation)
    }
}
