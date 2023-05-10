using MemCheck.Application.Images;
using MemCheck.Application.Notifiying;
using MemCheck.Application.QueryValidation;
using MemCheck.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemCheck.Application.Cards;

public sealed class CreateCard : RequestRunner<CreateCard.Request, CreateCard.Result>
{
    #region Fields
    private readonly DateTime? runDate;
    #endregion
    public CreateCard(CallContext callContext, DateTime? runDate = null) : base(callContext)
    {
        this.runDate = runDate;
    }
    protected override async Task<ResultWithMetrologyProperties<Result>> DoRunAsync(Request request)
    {
        var versionDate = runDate ?? DateTime.UtcNow;

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
            InitialCreationUtcDate = versionDate,
            VersionUtcDate = versionDate,
            VersionDescription = request.VersionDescription,
            LatestDiscussionEntry = null
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

        if (versionCreator.SubscribeToCardOnEdit)
            AddCardSubscriptions.CreateSubscription(DbContext, versionCreator.Id, card.Id, card.VersionUtcDate, CardNotificationSubscription.CardNotificationRegistrationMethodVersionCreation);

        var imageNames = ImageLoadingHelper.GetMnesiosImagesFromSides(card.FrontSide, card.BackSide, card.AdditionalInfo);
        foreach (var imageName in imageNames)
        {
            var image = await DbContext.Images.AsNoTracking().Select(image => new { image.Id, image.Name }).SingleOrDefaultAsync(image => EF.Functions.Like(image.Name, $"{imageName}"));
            if (image != null)
                DbContext.ImagesInCards.Add(new ImageInCard() { CardId = card.Id, ImageId = image.Id });
        }

        await DbContext.SaveChangesAsync();

        return new ResultWithMetrologyProperties<Result>(new Result(card.Id),
            ("CardId", card.Id.ToString()),
            ("Public", (!request.UsersWithVisibility.Any()).ToString()));
    }
    #region Request class
    public sealed class Request : ICardInput
    {
        public Request(Guid versionCreatorId, string frontSide, string backSide, string additionalInfo, string references, Guid languageId, IEnumerable<Guid> tags, IEnumerable<Guid> usersWithVisibility, string versionDescription)
        {
            VersionCreatorId = versionCreatorId;
            FrontSide = frontSide.Trim();
            BackSide = backSide.Trim();
            AdditionalInfo = additionalInfo.Trim();
            References = references.Trim();
            LanguageId = languageId;
            Tags = tags;
            UsersWithVisibility = usersWithVisibility;
            VersionDescription = versionDescription;
        }
        public Guid VersionCreatorId { get; }
        public string FrontSide { get; }
        public string BackSide { get; }
        public string AdditionalInfo { get; }
        public string References { get; }
        public Guid LanguageId { get; }
        public IEnumerable<Guid> Tags { get; }
        public IEnumerable<Guid> UsersWithVisibility { get; }
        public string VersionDescription { get; }
        public async Task CheckValidityAsync(CallContext callContext)
        {
            await CardInputValidator.RunAsync(this, callContext);
        }
    }
    public sealed record Result(Guid CardId);
    #endregion
}
