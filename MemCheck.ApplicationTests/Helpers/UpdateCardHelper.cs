using System;
using MemCheck.Domain;
using System.Collections.Generic;
using MemCheck.Application.CardChanging;
using System.Linq;

namespace MemCheck.Application.Tests.Helpers
{
    public static class UpdateCardHelper
    {
        public static UpdateCard.Request RequestForTagChanges(Card card, IEnumerable<Guid> tagIds)
        {
            return new UpdateCard.Request(
                card.Id,
                card.VersionCreator.Id,
                card.FrontSide,
                card.Images.Where(i => i.CardSide == ImageInCard.FrontSide).Select(i => i.ImageId),
                card.BackSide,
                card.Images.Where(i => i.CardSide == ImageInCard.BackSide).Select(i => i.ImageId),
                card.AdditionalInfo,
                card.Images.Where(i => i.CardSide == ImageInCard.AdditionalInfo).Select(i => i.ImageId),
                card.CardLanguage.Id,
                tagIds,
                card.UsersWithView.Select(uwv => uwv.UserId),
                StringServices.RandomString()
                );
        }
    }
}