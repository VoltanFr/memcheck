using System;
using System.ComponentModel.DataAnnotations;

namespace MemCheck.Domain
{
    //Entries of this class are deleted when an image is deleted. We don't keep track of deleted versions of images in card previous versions. (This is done in DeleteImage.DeleteFromCardPreviousVersions)
    public sealed class ImageInCardPreviousVersion
    {
        [Key] public Guid ImageId { get; set; }
        public Image Image { get; set; } = null!;

        [Key] public Guid CardPreviousVersionId { get; set; }
        public CardPreviousVersion CardPreviousVersion { get; set; } = null!;

        public int CardSide { get; set; }   //See constants in ImageInCard
    }
}
