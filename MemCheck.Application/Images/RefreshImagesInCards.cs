using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Images;

public sealed class RefreshImagesInCards : RequestRunner<RefreshImagesInCards.Request, RefreshImagesInCards.Result>
{
    private const string mnesiosImagePattern = "![Mnesios:";
    #region Fields
    #endregion
    public RefreshImagesInCards(CallContext callContext) : base(callContext)
    {
    }
    protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
    {
        var totalCardCount = await DbContext.Cards.CountAsync();

        var candidateCards = DbContext.Cards
            .AsNoTracking()
            .Where(card => card.FrontSide.Contains(mnesiosImagePattern) || card.BackSide.Contains(mnesiosImagePattern) || card.AdditionalInfo.Contains(mnesiosImagePattern))
            .Select(card => new { card.Id, card.FrontSide, card.BackSide, card.AdditionalInfo })
            .ToImmutableArray();

        var imageIdsFromNames = DbContext.Images
            .AsNoTracking()
            .Select(image => new { image.Id, image.Name })
            .ToImmutableDictionary(image => image.Name, image => image.Id);

        var imagesInCardsInTheDb = DbContext.ImagesInCards.ToImmutableArray();
        var imagesPerCardFromTheDb = imagesInCardsInTheDb.GroupBy(imageInCard => imageInCard.CardId)
            .ToImmutableDictionary(imagesInCard => imagesInCard.Key, imagesInCard => imagesInCard.Select(imageInCard => imageInCard).ToImmutableHashSet());

        var imagesInCardsIdsInTheDb = imagesPerCardFromTheDb
            .ToImmutableDictionary(keyValuePair => keyValuePair.Key, keyValuePair => keyValuePair.Value.Select(imageInCard => imageInCard.ImageId).ToImmutableHashSet());

        var changeCount = 0;

        foreach (var candidateCard in candidateCards)
        {
            var imageNamesInTheCard = ImageLoadingHelper.GetMnesiosImagesFromSides(candidateCard.FrontSide, candidateCard.BackSide, candidateCard.AdditionalInfo);
            var imageIdsInTheCard = imageNamesInTheCard.Where(imageName => imageIdsFromNames.ContainsKey(imageName)).Select(imageName => imageIdsFromNames[imageName]).ToImmutableArray();

            var imageIdsInThisCardInTheDbIds = imagesInCardsIdsInTheDb.TryGetValue(candidateCard.Id, out var value) ? value : ImmutableHashSet<Guid>.Empty;

            foreach (var imageIdInTheCard in imageIdsInTheCard)
                if (!imageIdsInThisCardInTheDbIds.Contains(imageIdInTheCard))
                {
                    DbContext.ImagesInCards.Add(new ImageInCard() { CardId = candidateCard.Id, ImageId = imageIdInTheCard });
                    changeCount++;
                }

            var imagesInThisCardInTheDb = imagesPerCardFromTheDb.TryGetValue(candidateCard.Id, out var fromDico) ? fromDico : ImmutableHashSet<ImageInCard>.Empty;

            foreach (var imageInThisCardInTheDb in imagesInThisCardInTheDb)
                if (!imageIdsInTheCard.Contains(imageInThisCardInTheDb.ImageId))
                {
                    DbContext.ImagesInCards.Remove(imageInThisCardInTheDb);
                    changeCount++;
                }
        }

        var imagesInCardsToDeleteCardIds = imagesInCardsInTheDb.Where(imageInCard => !candidateCards.Any(card => card.Id == imageInCard.CardId)).Select(imageInCard => imageInCard.CardId).ToImmutableHashSet();
        var imagesInCardsToDelete = DbContext.ImagesInCards.Where(imageInCard => imagesInCardsToDeleteCardIds.Contains(imageInCard.CardId));
        changeCount += imagesInCardsToDelete.Count();
        DbContext.ImagesInCards.RemoveRange(imagesInCardsToDelete);

        var nonExistingImagesInCardsInTheDb = DbContext.ImagesInCards.Where(imageInCard => !DbContext.Images.Any(img => img.Id == imageInCard.ImageId));
        changeCount += nonExistingImagesInCardsInTheDb.Count();
        DbContext.ImagesInCards.RemoveRange(nonExistingImagesInCardsInTheDb);

        await DbContext.SaveChangesAsync();

        var imagesInCardsCountOnEnd = await DbContext.ImagesInCards.CountAsync();

        return new ResultWithMetrologyProperties<Result>(new Result(imageIdsFromNames.Count, totalCardCount, imagesInCardsInTheDb.Length, imagesInCardsCountOnEnd, changeCount),
            IntMetric("ImageIdsFromNamesCount", imageIdsFromNames.Count),
            IntMetric("TotalCardCount", totalCardCount),
            IntMetric("ImagesInCardsInTheDbLength", imagesInCardsInTheDb.Length),
            IntMetric("ImagesInCardsCountOnEnd", imagesInCardsCountOnEnd),
            IntMetric("ChangeCount", changeCount)
            );
    }
    #region Request & Result
    public sealed record Request() : IRequest
    {
        public async Task CheckValidityAsync(CallContext callContext)
        {
            await Task.CompletedTask;
        }
    }
    public sealed record Result(int TotalImageCount, int TotalCardCount, int ImagesInCardsCountOnStart, int ImagesInCardsCountOnEnd, int ChangeCount);
    #endregion
}
