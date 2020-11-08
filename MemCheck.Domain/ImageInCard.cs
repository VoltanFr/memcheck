using System;
using System.ComponentModel.DataAnnotations;

namespace MemCheck.Domain
{
    public sealed class ImageInCard
    {
        [Key] public Guid ImageId { get; set; }
        public Image Image { get; set; } = null!;

        [Key] public Guid CardId { get; set; }
        public Card Card { get; set; } = null!;

        public int CardSide { get; set; }   //1 = front side ; 2 = back side ; 3 = AdditionalInfo
    }
}
