using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CanHappy.Models;

namespace CanHappy.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Subcategory> Subcategories => Set<Subcategory>();
    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<ListingImage> ListingImages => Set<ListingImage>();
    public DbSet<Ad> Ads => Set<Ad>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<Province> Provinces => Set<Province>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<UserMessage> UserMessages => Set<UserMessage>();

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

            entity.Property(e => e.TargetURL)
                .HasMaxLength(200);

            entity.Property(e => e.ImageURL)
                .HasMaxLength(200);

            entity.Property(e => e.CurrencyCode)
                .HasMaxLength(10);

            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Draft");

            entity.Property(e => e.AdSize)
                .HasMaxLength(15);

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
    }
}
