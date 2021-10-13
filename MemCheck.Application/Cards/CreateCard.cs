using MemCheck.Application.Notifying;
using MemCheck.Application.QueryValidation;
using MemCheck.Database;
using MemCheck.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards
{
    public sealed class CreateCard
    {
        #region Fields
        private readonly CallContext callContext;
        #endregion
        #region Private methods
        private void AddImage(Guid cardId, Guid imageId, int cardSide, List<ImageInCard> cardImageList)
        {
            var imageFromDb = callContext.DbContext.Images.Where(img => img.Id == imageId).Single();
            var img = new ImageInCard() { ImageId = imageId, Image = imageFromDb, CardId = cardId, CardSide = cardSide };
            callContext.DbContext.ImagesInCards.Add(img);
            cardImageList.Add(img);
        }
        #endregion
        public CreateCard(CallContext callContext)
        {
            this.callContext = callContext;
        }
        public async Task<Guid> RunAsync(Request request)
        {
            CardInputValidator.Run(request, callContext.Localized);

            var language = callContext.DbContext.CardLanguages.Where(language => language.Id == request.LanguageId).Single();
            var versionCreator = callContext.DbContext.Users.Where(user => user.Id == request.VersionCreatorId).Single();

            var card = new Card()
            {
                FrontSide = request.FrontSide,
                BackSide = request.BackSide,
                AdditionalInfo = request.AdditionalInfo,
                CardLanguage = language,
                VersionCreator = versionCreator,
                InitialCreationUtcDate = DateTime.Now.ToUniversalTime(),
                VersionUtcDate = DateTime.Now.ToUniversalTime(),
                VersionDescription = request.VersionDescription
            };
            callContext.DbContext.Cards.Add(card);

            var usersWithView = new List<UserWithViewOnCard>();
            foreach (var userFromRequestId in request.UsersWithVisibility)
            {
                var userFromDb = callContext.DbContext.Users.Where(u => u.Id == userFromRequestId).Single();
                var userWithView = new UserWithViewOnCard() { UserId = userFromDb.Id, User = userFromDb, CardId = card.Id, Card = card };
                callContext.DbContext.UsersWithViewOnCards.Add(userWithView);
                usersWithView.Add(userWithView);
            }
            card.UsersWithView = usersWithView;

            var tagsInCards = new List<TagInCard>();
            foreach (var tagToAdd in request.Tags)
            {
                var tagFromDb = callContext.DbContext.Tags.Where(t => t.Id == tagToAdd).Single();
                var tagInCard = new TagInCard() { TagId = tagFromDb.Id, Tag = tagFromDb, CardId = card.Id };
                callContext.DbContext.TagsInCards.Add(tagInCard);
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
                AddCardSubscriptions.CreateSubscription(callContext.DbContext, versionCreator.Id, card.Id, card.VersionUtcDate, CardNotificationSubscription.CardNotificationRegistrationMethod_VersionCreation);

            await callContext.DbContext.SaveChangesAsync();

            callContext.TelemetryClient.TrackEvent("CreateCard", ("CardId", card.Id.ToString()), ("Public", (!request.UsersWithVisibility.Any()).ToString()));

            return card.Id;
        }
        #region Request class
        public sealed class Request : ICardInput
        {
            public Request(Guid versionCreatorId, string frontSide, IEnumerable<Guid> frontSideImageList, string backSide, IEnumerable<Guid> backSideImageList, string additionalInfo, IEnumerable<Guid> additionalInfoImageList, Guid languageId, IEnumerable<Guid> tags, IEnumerable<Guid> usersWithVisibility, string versionDescription)
            {
                VersionCreatorId = versionCreatorId;
                FrontSide = frontSide.Trim();
                BackSide = backSide.Trim();
                AdditionalInfo = additionalInfo.Trim();
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
            public Guid LanguageId { get; }
            public IEnumerable<Guid> Tags { get; }
            public IEnumerable<Guid> UsersWithVisibility { get; }
            public string VersionDescription { get; }
        }
        #endregion
    }
}
