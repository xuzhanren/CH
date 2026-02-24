using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using CanHappy.Models;

namespace CanHappy.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Subcategory> Subcategories => Set<Subcategory>();
    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<ListingReview> ListingReviews => Set<ListingReview>();
    public DbSet<ListingReviewImage> ListingReviewImages => Set<ListingReviewImage>();
    public DbSet<ReviewReply> ReviewReplies => Set<ReviewReply>();
    public DbSet<BuySellDetail> BuySellDetails => Set<BuySellDetail>();
    public DbSet<ListingImage> ListingImages => Set<ListingImage>();
    public DbSet<Ad> Ads => Set<Ad>();
    public DbSet<AdSize> AdSizes => Set<AdSize>();
    public DbSet<AdStatus> AdStatuses => Set<AdStatus>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<Province> Provinces => Set<Province>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<UserMessage> UserMessages => Set<UserMessage>();

    public override int SaveChanges()
    {
        NormalizeDateTimesToUtc();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        NormalizeDateTimesToUtc();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        NormalizeDateTimesToUtc();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        NormalizeDateTimesToUtc();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Category>(entity =>
        {
            entity.ToTable("Category");
            entity.HasKey(e => e.CategoryId);

            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Code)
                .HasMaxLength(50);

            entity.Property(e => e.Description)
                .HasMaxLength(100);

            entity.Property(e => e.KeyWords)
                .HasMaxLength(800);

            entity.Property(e => e.KeyWords)
                .HasMaxLength(800);

            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100);

            entity.Property(e => e.ModifiedBy)
                .HasMaxLength(100);

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        builder.Entity<Subcategory>(entity =>
        {
            entity.ToTable("Subcategory");
            entity.HasKey(e => e.SubcategoryId);

            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Code)
                .HasMaxLength(50);

            entity.Property(e => e.Description)
                .HasMaxLength(100);

            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100);

            entity.Property(e => e.ModifiedBY)
                .HasMaxLength(100);

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => new { e.CategoryId, e.Name })
                .IsUnique();

            entity.HasOne(e => e.Category)
                .WithMany()
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Ad>(entity =>
        {
            entity.ToTable("Ad");
            entity.HasKey(e => e.AdGUID);

            entity.Property(e => e.PostalCode)
                .HasMaxLength(15);

            entity.Property(e => e.Subject)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasMaxLength(200);

            entity.Property(e => e.KeyWords)
                .HasMaxLength(100);

            entity.Property(e => e.TargetURL)
                .HasMaxLength(200);

            entity.Property(e => e.ImageURL)
                .HasMaxLength(200);

            entity.Property(e => e.CurrencyCode)
                .HasMaxLength(10);

            entity.Property(e => e.AdStatusId)
                .HasDefaultValue(1);

            entity.Property(e => e.Price)
                .HasPrecision(18, 2);

            entity.Property(e => e.ContactName)
                .HasMaxLength(100);

            entity.Property(e => e.ContactEmail)
                .HasMaxLength(100);

            entity.Property(e => e.ContactPhone)
                .HasMaxLength(30);

            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100);

            entity.Property(e => e.ModifiedBY)
                .HasMaxLength(100);

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.PublishDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.CityId);
            entity.HasIndex(e => e.CategoryId);
            entity.HasIndex(e => e.SubcategoryId);
            entity.HasIndex(e => e.ProvinceId);
            entity.HasIndex(e => e.AdStatusId);
            entity.HasIndex(e => e.AdSizeId);
            entity.HasIndex(e => new { e.CityId, e.DeletedInd, e.PublishDate });

            entity.HasOne(e => e.Category)
                .WithMany()
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Subcategory)
                .WithMany()
                .HasForeignKey(e => e.SubcategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Province)
                .WithMany()
                .HasForeignKey(e => e.ProvinceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.City)
                .WithMany()
                .HasForeignKey(e => e.CityId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.AdStatus)
                .WithMany()
                .HasForeignKey(e => e.AdStatusId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.AdSizeOption)
                .WithMany()
                .HasForeignKey(e => e.AdSizeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AdSize>(entity =>
        {
            entity.ToTable("AdSize");
            entity.HasKey(e => e.AdSizeId);

            entity.Property(e => e.Name)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasMaxLength(100);

            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50);

            entity.Property(e => e.ModifiedBy)
                .HasMaxLength(50);

            entity.Property(e => e.DeletedInd)
                .HasDefaultValue(false);

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.Name)
                .IsUnique();

            entity.HasData(
                new AdSize
                {
                    AdSizeId = 1,
                    Name = "300x250",
                    Description = "Medium Rectangle",
                    DeletedInd = false,
                    CreatedBy = "system",
                    ModifiedBy = null,
                    CreatedDate = new DateTime(2026, 2, 22, 0, 0, 0, DateTimeKind.Utc),
                    ModifiedDate = null
                },
                new AdSize
                {
                    AdSizeId = 2,
                    Name = "320x50",
                    Description = "Mobile Banner",
                    DeletedInd = false,
                    CreatedBy = "system",
                    ModifiedBy = null,
                    CreatedDate = new DateTime(2026, 2, 22, 0, 0, 0, DateTimeKind.Utc),
                    ModifiedDate = null
                },
                new AdSize
                {
                    AdSizeId = 3,
                    Name = "728x90",
                    Description = "Leaderboard",
                    DeletedInd = false,
                    CreatedBy = "system",
                    ModifiedBy = null,
                    CreatedDate = new DateTime(2026, 2, 22, 0, 0, 0, DateTimeKind.Utc),
                    ModifiedDate = null
                },
                new AdSize
                {
                    AdSizeId = 4,
                    Name = "1200x628",
                    Description = "Social Share Image",
                    DeletedInd = false,
                    CreatedBy = "system",
                    ModifiedBy = null,
                    CreatedDate = new DateTime(2026, 2, 22, 0, 0, 0, DateTimeKind.Utc),
                    ModifiedDate = null
                });
        });

        builder.Entity<AdStatus>(entity =>
        {
            entity.ToTable("AdStatus");
            entity.HasKey(e => e.AdStatusId);

            entity.Property(e => e.Name)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasMaxLength(100);

            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50);

            entity.Property(e => e.ModifiedBy)
                .HasMaxLength(50);

            entity.Property(e => e.DeletedInd)
                .HasDefaultValue(false);

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.Name)
                .IsUnique();

            entity.HasData(
                new AdStatus
                {
                    AdStatusId = 1,
                    Name = "Draft",
                    Description = "Work in progress",
                    DeletedInd = false,
                    CreatedBy = "system",
                    ModifiedBy = null,
                    CreatedDate = new DateTime(2026, 2, 22, 0, 0, 0, DateTimeKind.Utc),
                    ModifiedDate = null
                },
                new AdStatus
                {
                    AdStatusId = 2,
                    Name = "Design finalized",
                    Description = "Creative approved",
                    DeletedInd = false,
                    CreatedBy = "system",
                    ModifiedBy = null,
                    CreatedDate = new DateTime(2026, 2, 22, 0, 0, 0, DateTimeKind.Utc),
                    ModifiedDate = null
                },
                new AdStatus
                {
                    AdStatusId = 3,
                    Name = "Released to show",
                    Description = "Active and visible",
                    DeletedInd = false,
                    CreatedBy = "system",
                    ModifiedBy = null,
                    CreatedDate = new DateTime(2026, 2, 22, 0, 0, 0, DateTimeKind.Utc),
                    ModifiedDate = null
                },
                new AdStatus
                {
                    AdStatusId = 4,
                    Name = "Stopped from show",
                    Description = "Temporarily inactive",
                    DeletedInd = false,
                    CreatedBy = "system",
                    ModifiedBy = null,
                    CreatedDate = new DateTime(2026, 2, 22, 0, 0, 0, DateTimeKind.Utc),
                    ModifiedDate = null
                },
                new AdStatus
                {
                    AdStatusId = 5,
                    Name = "Decommssioned",
                    Description = "Permanently retired",
                    DeletedInd = false,
                    CreatedBy = "system",
                    ModifiedBy = null,
                    CreatedDate = new DateTime(2026, 2, 22, 0, 0, 0, DateTimeKind.Utc),
                    ModifiedDate = null
                });
        });

        builder.Entity<Listing>(entity =>
        {
            entity.ToTable("Listing");
            entity.HasKey(e => e.ListingGUID);

            entity.Property(e => e.Subject)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.Property(e => e.KeyWords)
                .HasMaxLength(50);

            entity.Property(e => e.PostalCode)
                .HasMaxLength(15);

            entity.Property(e => e.ThumbnailURL)
                .HasMaxLength(200);

            entity.Property(e => e.Rating)
                .HasMaxLength(5)
                .HasDefaultValue("3.5");

            entity.Property(e => e.Brand)
                .HasMaxLength(50);

            entity.Property(e => e.Condition)
                .HasMaxLength(10)
                .HasDefaultValue("Used");

            entity.Property(e => e.Model)
                .HasMaxLength(30);

            entity.Property(e => e.Quantity)
                .HasDefaultValue(1);

            entity.Property(e => e.Price)
                .HasPrecision(18, 2);

            entity.Property(e => e.DiscountPercent)
                .HasPrecision(5, 2)
                .HasDefaultValue(0.00m);

            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100);

            entity.Property(e => e.ModifiedBY)
                .HasMaxLength(100);

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.CategoryId);
            entity.HasIndex(e => e.SubcategoryId);
            entity.HasIndex(e => e.ProvinceId);
            entity.HasIndex(e => e.CityId);
            entity.HasIndex(e => new { e.CityId, e.DeletedInd, e.CreatedDate });

            entity.HasOne(e => e.Category)
                .WithMany()
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Subcategory)
                .WithMany()
                .HasForeignKey(e => e.SubcategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Province)
                .WithMany()
                .HasForeignKey(e => e.ProvinceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.City)
                .WithMany()
                .HasForeignKey(e => e.CityId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ListingReview>(entity =>
        {
            entity.ToTable("ListingReview");
            entity.HasKey(e => e.ListingReviewGUID);

            entity.Property(e => e.Rating)
                .HasPrecision(2, 1);

            entity.Property(e => e.ReviewTitle)
                .HasMaxLength(120);

            entity.Property(e => e.ReviewMessage)
                .HasMaxLength(500);

            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValue(ReviewWorkflowStatus.Submitted);

            entity.Property(e => e.DeletedInd)
                .HasDefaultValue(false);

            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50);

            entity.Property(e => e.ModifiedBy)
                .HasMaxLength(50);

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.ListingGUID);
            entity.HasIndex(e => new { e.ListingGUID, e.DeletedInd, e.CreatedDate });

            entity.HasOne(e => e.Listing)
                .WithMany(e => e.ListingReviews)
                .HasForeignKey(e => e.ListingGUID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ListingReviewImage>(entity =>
        {
            entity.ToTable("ListingReviewImage");
            entity.HasKey(e => e.ListingReviewImageGUID);

            entity.Property(e => e.Title)
                .HasMaxLength(100);

            entity.Property(e => e.ThumbnailURL)
                .HasMaxLength(200);

            entity.Property(e => e.ImageURL)
                .HasMaxLength(200);

            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50);

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.DeletedInd)
                .HasDefaultValue(false);

            entity.HasIndex(e => e.ListingReviewGUID);

            entity.HasOne(e => e.ListingReview)
                .WithMany(e => e.Images)
                .HasForeignKey(e => e.ListingReviewGUID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ReviewReply>(entity =>
        {
            entity.ToTable("ReviewReply");
            entity.HasKey(e => e.ReviewReplyGUID);

            entity.Property(e => e.ReplyMessage)
                .HasMaxLength(500);

            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValue(ReviewWorkflowStatus.Submitted);

            entity.Property(e => e.DeletedInd)
                .HasDefaultValue(false);

            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50);

            entity.Property(e => e.ModifiedBy)
                .HasMaxLength(50);

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.ListingReviewGUID);
            entity.HasIndex(e => new { e.ListingReviewGUID, e.DeletedInd, e.CreatedDate });

            entity.HasOne(e => e.ListingReview)
                .WithMany(e => e.Replies)
                .HasForeignKey(e => e.ListingReviewGUID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ListingImage>(entity =>
        {
            entity.ToTable("ListingImage");
            entity.HasKey(e => e.ListingImageGUID);

            entity.Property(e => e.Title)
                .HasMaxLength(50);

            entity.Property(e => e.ThumbnailURL)
                .HasMaxLength(200);

            entity.Property(e => e.ImageURL)
                .HasMaxLength(200);

            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100);

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.ListingGUID);

            entity.HasOne(e => e.Listing)
                .WithMany()
                .HasForeignKey(e => e.ListingGUID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<BuySellDetail>(entity =>
        {
            entity.ToTable("BuySellDetail");
            entity.HasKey(e => e.BuySellDetailGUID);

            entity.Property(e => e.Model)
                .HasMaxLength(50);

            entity.Property(e => e.PostalCode)
                .HasMaxLength(30);

            entity.Property(e => e.Material)
                .HasMaxLength(50);

            entity.Property(e => e.WarrantyInfo)
                .HasMaxLength(100);

            entity.Property(e => e.AdditionalDetails)
                .HasMaxLength(500);

            entity.Property(e => e.PickupTime)
                .HasMaxLength(30);

            entity.Property(e => e.PickupLocation)
                .HasMaxLength(50);

            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100);

            entity.Property(e => e.ModifiedBY)
                .HasMaxLength(100);

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.ListingGUID)
                .IsUnique();

            entity.HasOne(e => e.Listing)
                .WithOne(e => e.BuySellDetail)
                .HasForeignKey<BuySellDetail>(e => e.ListingGUID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<UserMessage>(entity =>
        {
            entity.ToTable("UserMessage");
            entity.HasKey(e => e.UserMessageGUID);

            entity.Property(e => e.Subject)
                .HasMaxLength(120)
                .IsRequired();

            entity.Property(e => e.Body)
                .HasMaxLength(2000)
                .IsRequired();

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.SenderUserId);
            entity.HasIndex(e => e.RecipientUserId);
            entity.HasIndex(e => e.ListingGUID);
            entity.HasIndex(e => new { e.RecipientUserId, e.CreatedDate });

            entity.HasOne(e => e.Listing)
                .WithMany()
                .HasForeignKey(e => e.ListingGUID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Country>(entity =>
        {
            entity.ToTable("Country");
            entity.HasKey(e => e.CountryId);

            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Code)
                .HasMaxLength(50);

            entity.Property(e => e.Description)
                .HasMaxLength(100);

            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100);

            entity.Property(e => e.ModifiedBY)
                .HasMaxLength(100);

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        builder.Entity<Province>(entity =>
        {
            entity.ToTable("Province");
            entity.HasKey(e => e.ProvinceId);

            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Code)
                .HasMaxLength(50);

            entity.Property(e => e.Description)
                .HasMaxLength(100);

            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100);

            entity.Property(e => e.ModifiedBY)
                .HasMaxLength(100);

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => new { e.CountryId, e.Name })
                .IsUnique();

            entity.HasOne(e => e.Country)
                .WithMany(e => e.Provinces)
                .HasForeignKey(e => e.CountryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<City>(entity =>
        {
            entity.ToTable("City");
            entity.HasKey(e => e.CityId);

            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Code)
                .HasMaxLength(50);

            entity.Property(e => e.Description)
                .HasMaxLength(100);

            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100);

            entity.Property(e => e.ModifiedBY)
                .HasMaxLength(100);

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => new { e.ProvinceId, e.Name })
                .IsUnique();

            entity.HasOne(e => e.Province)
                .WithMany(e => e.Cities)
                .HasForeignKey(e => e.ProvinceId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Area>(entity =>
        {
            entity.ToTable("Area");
            entity.HasKey(e => e.AreaId);

            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Code)
                .HasMaxLength(50);

            entity.Property(e => e.Description)
                .HasMaxLength(100);

            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100);

            entity.Property(e => e.ModifiedBY)
                .HasMaxLength(100);

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => new { e.CityId, e.Name })
                .IsUnique();

            entity.HasOne(e => e.City)
                .WithMany(e => e.Areas)
                .HasForeignKey(e => e.CityId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        ApplyEasternDateTimeConverters(builder);
    }

    private static void ApplyEasternDateTimeConverters(ModelBuilder builder)
    {
        var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
            value => CanHappy.Common.EasternTime.ToUtc(value),
            value => CanHappy.Common.EasternTime.ToEastern(value));

        var nullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
            value => value.HasValue ? CanHappy.Common.EasternTime.ToUtc(value.Value) : value,
            value => value.HasValue ? CanHappy.Common.EasternTime.ToEastern(value.Value) : value);

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(dateTimeConverter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(nullableDateTimeConverter);
                }
            }
        }
    }

    private void NormalizeDateTimesToUtc()
    {
        foreach (var entry in ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified))
        {
            foreach (var property in entry.Properties)
            {
                if (property.Metadata.ClrType == typeof(DateTime) && property.CurrentValue is DateTime dateTimeValue)
                {
                    property.CurrentValue = CanHappy.Common.EasternTime.ToUtc(dateTimeValue);
                }
                else if (property.Metadata.ClrType == typeof(DateTime?) && property.CurrentValue is DateTime nullableDateTimeValue)
                {
                    property.CurrentValue = CanHappy.Common.EasternTime.ToUtc(nullableDateTimeValue);
                }
            }
        }
    }
}
