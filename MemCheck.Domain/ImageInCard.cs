using System;
using System.ComponentModel.DataAnnotations;

namespace MemCheck.Domain;

public sealed class ImageInCard
{
    public const int FrontSide = 1;
    public const int BackSide = 2;
    public const int AdditionalInfo = 3;

    [Key] public Guid ImageId { get; set; }
    public Image Image { get; set; } = null!;

    [Key] public Guid CardId { get; set; }
    public Card Card { get; set; } = null!;

    public int CardSide { get; set; }   //One of the constants above
}
