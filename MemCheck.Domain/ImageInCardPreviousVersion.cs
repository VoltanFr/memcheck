using System;
using System.ComponentModel.DataAnnotations;

namespace MemCheck.Domain
{
    public sealed class ImageInCardPreviousVersion
    {
        [Key] public Guid ImageId { get; set; }
        public Image Image { get; set; } = null!;

        [Key] public Guid CardPreviousVersionId { get; set; }
        public CardPreviousVersion CardPreviousVersion { get; set; } = null!;

        public int CardSide { get; set; }   //1 = front side ; 2 = back side ; 3 = AdditionalInfo
    }
}
