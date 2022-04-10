using MemCheck.Application.Notifiying;
using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards
{
    public sealed class CreateCard : RequestRunner<CreateCard.Request, CreateCard.Result>
    {
        #region Private methods
        private void AddImage(Guid cardId, Guid imageId, int cardSide, List<ImageInCard> cardImageList)
        {
            var imageFromDb = DbContext.Images.Where(img => img.Id == imageId).Single();
            var img = new ImageInCard() { ImageId = imageId, Image = imageFromDb, CardId = cardId, CardSide = cardSide };
            DbContext.ImagesInCards.Add(img);
            cardImageList.Add(img);
        }
        #endregion
        public CreateCard(CallContext callContext) : base(callContext)
        {
        }
        protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
        {
            var language = DbContext.CardLanguages.Where(language => language.Id == request.LanguageId).Single();
            var versionCreator = DbContext.Users.Where(user => user.Id == request.VersionCreatorId).Single();

            var card = new Card()
            {
                FrontSide = request.FrontSide,
                BackSide = request.BackSide,
                AdditionalInfo = request.AdditionalInfo,
                References = request.References,
                CardLanguage = language,
                VersionCreator = versionCreator,
                InitialCreationUtcDate = DateTime.Now.ToUniversalTime(),
                VersionUtcDate = DateTime.Now.ToUniversalTime(),
                VersionDescription = request.VersionDescription
            };
            DbContext.Cards.Add(card);

            var usersWithView = new List<UserWithViewOnCard>();
            foreach (var userFromRequestId in request.UsersWithVisibility)
            {
                var userFromDb = DbContext.Users.Where(u => u.Id == userFromRequestId).Single();
                var userWithView = new UserWithViewOnCard() { UserId = userFromDb.Id, User = userFromDb, CardId = card.Id, Card = card };
                DbContext.UsersWithViewOnCards.Add(userWithView);
                usersWithView.Add(userWithView);
            }
            card.UsersWithView = usersWithView;

            var tagsInCards = new List<TagInCard>();
            foreach (var tagToAdd in request.Tags)
            {
                var tagFromDb = DbContext.Tags.Where(t => t.Id == tagToAdd).Single();
                var tagInCard = new TagInCard() { TagId = tagFromDb.Id, Tag = tagFromDb, CardId = card.Id };
                DbContext.TagsInCards.Add(tagInCard);
                tagsInCards.Add(tagInCard);
            }
            card.TagsInCards = tagsInCards;

            var cardImageList = new List<ImageInCard>();
            foreach (var image in request.FrontSideImageList)
                AddImage(card.Id, image, 1, cardImageList);
            foreach (var image in request.BackSideImageList)
                AddImage(card.Id, image, 2, cardImageList);
            foreach (var image in request.AdditionalInfoImageList)
                AddImage(card.Id, image, 3, cardImageList);
            card.Images = cardImageList;

            if (versionCreator.SubscribeToCardOnEdit)
                AddCardSubscriptions.CreateSubscription(DbContext, versionCreator.Id, card.Id, card.VersionUtcDate, CardNotificationSubscription.CardNotificationRegistrationMethodVersionCreation);

            await DbContext.SaveChangesAsync();

            return new ResultWithMetrologyProperties<Result>(new Result(card.Id),
                ("CardId", card.Id.ToString()),
                ("Public", (!request.UsersWithVisibility.Any()).ToString()));
        }
        #region Request class
        public sealed class Request : ICardInput
        {
            public Request(Guid versionCreatorId, string frontSide, IEnumerable<Guid> frontSideImageList, string backSide, IEnumerable<Guid> backSideImageList, string additionalInfo, IEnumerable<Guid> additionalInfoImageList, string references, Guid languageId, IEnumerable<Guid> tags, IEnumerable<Guid> usersWithVisibility, string versionDescription)
            {
                VersionCreatorId = versionCreatorId;
                FrontSide = frontSide.Trim();
                BackSide = backSide.Trim();
                AdditionalInfo = additionalInfo.Trim();
                References = references.Trim();
                FrontSideImageList = frontSideImageList;
                BackSideImageList = backSideImageList;
                AdditionalInfoImageList = additionalInfoImageList;
                LanguageId = languageId;
                Tags = tags;
                UsersWithVisibility = usersWithVisibility;
                VersionDescription = versionDescription;
            }
            public Guid VersionCreatorId { get; }
            public string FrontSide { get; }
            public IEnumerable<Guid> FrontSideImageList { get; }
            public string BackSide { get; }
            public IEnumerable<Guid> BackSideImageList { get; }
            public string AdditionalInfo { get; }
            public IEnumerable<Guid> AdditionalInfoImageList { get; }
            public string References { get; }
            public Guid LanguageId { get; }
            public IEnumerable<Guid> Tags { get; }
            public IEnumerable<Guid> UsersWithVisibility { get; }
            public string VersionDescription { get; }
            public async Task CheckValidityAsync(CallContext callContext)
            {
                CardInputValidator.Run(this, callContext.Localized);
                await Task.CompletedTask;
            }
        }
        public sealed record Result(Guid CardId);
        #endregion
    }
}
