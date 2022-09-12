using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MemCheck.Domain;

public enum CardVersionType { Creation, Changes }

public sealed class Card
{
    [Key] public Guid Id { get; set; }
    public MemCheckUser VersionCreator { get; set; } = null!;
    public CardLanguage CardLanguage { get; set; } = null!;
    public string FrontSide { get; set; } = null!;
    public string BackSide { get; set; } = null!;
    public string AdditionalInfo { get; set; } = null!;
    public string References { get; set; } = null!;
    public IEnumerable<CardInDeck> CardInDecks { get; set; } = null!;
    public IEnumerable<TagInCard> TagsInCards { get; set; } = null!;
    public IEnumerable<ImageInCard> ImagesInCards { get; set; } = null!;
    public DateTime InitialCreationUtcDate { get; set; } //This field is immutable accross versions, but keeping it avoids the need to walk all the versions to find the initial creation date
    public DateTime VersionUtcDate { get; set; }
    public IEnumerable<UserWithViewOnCard> UsersWithView { get; set; } = null!; //Empty list means public ; If not empty, version creator has to be in the list (only version creator in list means private)
    public string VersionDescription { get; set; } = null!;
    [Column(TypeName = "int")] public CardVersionType VersionType { get; set; }
    public CardPreviousVersion? PreviousVersion { get; set; }  //null for initial version (VersionType == Creation)
    public int RatingCount { get; set; } //Number of ratings for this card (ie number of users who have set a rating)
    public double AverageRating { get; set; } //The average of all ratings for this card (in the table UserCardRating). A rating is between 1 and 5, so the average is in this interval too. An average of 0 means that RatingCount is 0
    public IEnumerable<UserCardRating> UserCardRating { get; set; } = null!;
}
