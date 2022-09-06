using MemCheck.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;

namespace MemCheck.Database;
public class MemCheckDbContext : IdentityDbContext<MemCheckUser, MemCheckUserRole, Guid>
{
    #region Private methods
    private static void EnforceAllDatesUtc(ModelBuilder builder)
    {
        var utcConverter = new ValueConverter<DateTime, DateTime>(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        foreach (var entityType in builder.Model.GetEntityTypes())
            foreach (var property in entityType.GetProperties())
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                    property.SetValueConverter(utcConverter);
    }
    #endregion
    public MemCheckDbContext(DbContextOptions options)
        : base(options)
    {
    }
    public DbSet<Card> Cards { get; set; } = null!;
    public DbSet<CardPreviousVersion> CardPreviousVersions { get; set; } = null!;
    public DbSet<Deck> Decks { get; set; } = null!;
    public DbSet<Tag> Tags { get; set; } = null!;
    public DbSet<CardInDeck> CardsInDecks { get; set; } = null!;
    public DbSet<TagInCard> TagsInCards { get; set; } = null!;
    public DbSet<TagInPreviousCardVersion> TagInPreviousCardVersions { get; set; } = null!;
    public DbSet<CardLanguage> CardLanguages { get; set; } = null!;
    public DbSet<UserWithViewOnCard> UsersWithViewOnCards { get; set; } = null!;
    public DbSet<Image> Images { get; set; } = null!;
    public DbSet<ImagePreviousVersion> ImagePreviousVersions { get; set; } = null!;
    public DbSet<UserWithViewOnCardPreviousVersion> UsersWithViewOnCardPreviousVersions { get; set; } = null!;
    public DbSet<UserCardRating> UserCardRatings { get; set; } = null!;
    public DbSet<CardNotificationSubscription> CardNotifications { get; set; } = null!;
    public DbSet<SearchSubscription> SearchSubscriptions { get; set; } = null!;
    public DbSet<RequiredTagInSearchSubscription> RequiredTagInSearchSubscriptions { get; set; } = null!;
    public DbSet<ExcludedTagInSearchSubscription> ExcludedTagInSearchSubscriptions { get; set; } = null!;
    public DbSet<CardInSearchResult> CardsInSearchResults { get; set; } = null!;
    public DbSet<DemoDownloadAuditTrailEntry> DemoDownloadAuditTrailEntries { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        CreateCompositePrimaryKeys(builder);
        AddIndexesRecomendedByAzureWebSite(builder);
        EnforceAllDatesUtc(builder);
    }
    private static void CreateCompositePrimaryKeys(ModelBuilder builder)
    {
        builder.Entity<CardInDeck>().HasKey(cardInDeck => new { cardInDeck.CardId, cardInDeck.DeckId });
        builder.Entity<CardInDeck>().HasOne(e => e.Card).WithMany(e => e.CardInDecks).HasForeignKey(e => e.CardId).OnDelete(DeleteBehavior.NoAction);
        builder.Entity<CardInDeck>().HasOne(e => e.Deck).WithMany(e => e.CardInDecks).HasForeignKey(e => e.DeckId).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<TagInCard>().HasKey(tagInCard => new { tagInCard.CardId, tagInCard.TagId });

        builder.Entity<UserWithViewOnCard>().HasKey(userWithViewOnCard => new { userWithViewOnCard.CardId, userWithViewOnCard.UserId });
        builder.Entity<UserWithViewOnCard>().HasOne(e => e.Card).WithMany(e => e.UsersWithView).HasForeignKey(e => e.CardId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<UserWithViewOnCard>().HasOne(e => e.User).WithMany(e => e.UsersWithView).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Image>().HasIndex(img => img.Name).IsUnique();
        builder.Entity<TagInPreviousCardVersion>().HasKey(tagInPreviousCardVersion => new { tagInPreviousCardVersion.CardPreviousVersionId, tagInPreviousCardVersion.TagId });

        builder.Entity<UserWithViewOnCardPreviousVersion>().HasKey(userWithViewOnCardPreviousVersion => new { userWithViewOnCardPreviousVersion.CardPreviousVersionId, userWithViewOnCardPreviousVersion.AllowedUserId });
        builder.Entity<UserWithViewOnCardPreviousVersion>().HasOne(e => e.CardPreviousVersion).WithMany(e => e.UsersWithView).HasForeignKey(e => e.CardPreviousVersionId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<UserWithViewOnCardPreviousVersion>().HasOne(e => e.AllowedUser).WithMany(e => e.UsersWithViewOnPreviousVersion).HasForeignKey(e => e.AllowedUserId).OnDelete(DeleteBehavior.NoAction);

        builder.Entity<UserCardRating>().HasKey(userCardRating => new { userCardRating.CardId, userCardRating.UserId });
        builder.Entity<UserCardRating>().HasOne(e => e.Card).WithMany(e => e.UserCardRating).HasForeignKey(e => e.CardId).OnDelete(DeleteBehavior.NoAction);
        builder.Entity<UserCardRating>().HasOne(e => e.User).WithMany(e => e.UserCardRating).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.NoAction);

        builder.Entity<CardNotificationSubscription>().HasKey(cardNotif => new { cardNotif.CardId, cardNotif.UserId });
        builder.Entity<RequiredTagInSearchSubscription>().HasKey(requiredTag => new { requiredTag.SearchSubscriptionId, requiredTag.TagId });
        builder.Entity<ExcludedTagInSearchSubscription>().HasKey(requiredTag => new { requiredTag.SearchSubscriptionId, requiredTag.TagId });
        builder.Entity<CardInSearchResult>().HasKey(cardInSearchResult => new { cardInSearchResult.SearchSubscriptionId, cardInSearchResult.CardId });
    }
    private static void AddIndexesRecomendedByAzureWebSite(ModelBuilder builder)
    {
        builder.Entity<CardInDeck>().HasIndex(cardInDeck => cardInDeck.CurrentHeap);

        builder.Entity<CardInDeck>(entity =>
        {
            entity.HasIndex(e => new { e.DeckId, e.CurrentHeap, e.CardId })
            .IncludeProperties(e => new { e.AddToDeckUtcTime, e.BiggestHeapReached, e.LastLearnUtcTime, e.NbTimesInNotLearnedHeap });

        });

        builder.Entity<CardInDeck>(entity =>
        {
            entity.HasIndex(e => new { e.DeckId, e.CurrentHeap, e.ExpiryUtcTime })
            .IncludeProperties(e => new { e.AddToDeckUtcTime, e.BiggestHeapReached, e.CardId, e.LastLearnUtcTime, e.NbTimesInNotLearnedHeap });
        });

        builder.Entity<UserCardRating>(entity =>
        {
            entity.HasIndex(e => new { e.UserId })
            .IncludeProperties(e => new { e.Rating });

        });
    }
}
