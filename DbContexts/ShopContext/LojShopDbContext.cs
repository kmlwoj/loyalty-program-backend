using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace lojalBackend.DbContexts.ShopContext;

public partial class LojShopDbContext : DbContext
{
    public LojShopDbContext()
    {
    }

    public LojShopDbContext(DbContextOptions<LojShopDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Code> Codes { get; set; }

    public virtual DbSet<Discount> Discounts { get; set; }

    public virtual DbSet<Offer> Offers { get; set; }

    public virtual DbSet<Organization> Organizations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Name).HasName("PRIMARY");

            entity.ToTable("CATEGORIES");

            entity.Property(e => e.Name)
                .HasMaxLength(128)
                .HasColumnName("NAME");
        });

        modelBuilder.Entity<Code>(entity =>
        {
            entity.HasKey(e => new { e.CodeId, e.OfferId }).HasName("PRIMARY");

            entity.ToTable("CODES");

            entity.HasIndex(e => e.OfferId, "OFFER_ID");

            entity.Property(e => e.CodeId).HasColumnName("CODE_ID");
            entity.Property(e => e.OfferId).HasColumnName("OFFER_ID");
            entity.Property(e => e.Expiry)
                .HasColumnType("timestamp")
                .HasColumnName("EXPIRY");
            entity.Property(e => e.State)
                .HasDefaultValueSql("b'1'")
                .HasColumnType("bit(1)")
                .HasColumnName("STATE");

            entity.HasOne(d => d.Offer).WithMany(p => p.Codes)
                .HasForeignKey(d => d.OfferId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("CODES_ibfk_1");
        });

        modelBuilder.Entity<Discount>(entity =>
        {
            entity.HasKey(e => e.DiscId).HasName("PRIMARY");

            entity.ToTable("DISCOUNTS");

            entity.HasIndex(e => e.OfferId, "OFFER_ID");

            entity.Property(e => e.DiscId).HasColumnName("DISC_ID");
            entity.Property(e => e.Expiry)
                .HasColumnType("timestamp")
                .HasColumnName("EXPIRY");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("NAME");
            entity.Property(e => e.OfferId).HasColumnName("OFFER_ID");
            entity.Property(e => e.Reduction)
                .HasMaxLength(255)
                .HasColumnName("REDUCTION");

            entity.HasOne(d => d.Offer).WithMany(p => p.Discounts)
                .HasForeignKey(d => d.OfferId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("DISCOUNTS_ibfk_1");
        });

        modelBuilder.Entity<Offer>(entity =>
        {
            entity.HasKey(e => e.OfferId).HasName("PRIMARY");

            entity.ToTable("OFFERS");

            entity.HasIndex(e => e.Category, "CATEGORY");

            entity.HasIndex(e => e.Organization, "ORGANIZATION");

            entity.Property(e => e.OfferId).HasColumnName("OFFER_ID");
            entity.Property(e => e.Category)
                .HasMaxLength(128)
                .HasColumnName("CATEGORY");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("NAME");
            entity.Property(e => e.Organization).HasColumnName("ORGANIZATION");
            entity.Property(e => e.Price).HasColumnName("PRICE");
            entity.Property(e => e.State)
                .HasDefaultValueSql("b'1'")
                .HasColumnType("bit(1)")
                .HasColumnName("STATE");

            entity.HasOne(d => d.CategoryNavigation).WithMany(p => p.Offers)
                .HasForeignKey(d => d.Category)
                .HasConstraintName("OFFERS_ibfk_2");

            entity.HasOne(d => d.OrganizationNavigation).WithMany(p => p.Offers)
                .HasForeignKey(d => d.Organization)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("OFFERS_ibfk_1");
        });

        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasKey(e => e.Name).HasName("PRIMARY");

            entity.ToTable("ORGANIZATIONS");

            entity.Property(e => e.Name).HasColumnName("NAME");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
