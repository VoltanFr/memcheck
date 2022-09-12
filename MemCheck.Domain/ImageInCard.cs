using System;

namespace MemCheck.Domain;

public sealed class ImageInCard
{
    public Guid ImageId { get; set; }
    public Image Image { get; set; } = null!;

    public Guid CardId { get; set; }
    public Card Card { get; set; } = null!;
}
