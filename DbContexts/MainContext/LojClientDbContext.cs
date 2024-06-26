﻿using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace lojalBackend.DbContexts.MainContext;

public partial class LojClientDbContext : DbContext
{
    public LojClientDbContext()
    {
    }

    public LojClientDbContext(DbContextOptions<LojClientDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Code> Codes { get; set; }

    public virtual DbSet<ContactInfo> ContactInfos { get; set; }

    public virtual DbSet<ContactRequest> ContactRequests { get; set; }

    public virtual DbSet<Discount> Discounts { get; set; }

    public virtual DbSet<Offer> Offers { get; set; }

    public virtual DbSet<Organization> Organizations { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<Transaction> Transactions { get; set; }

    public virtual DbSet<User> Users { get; set; }

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

            entity.HasIndex(e => e.DiscId, "DISC_ID");

            entity.HasIndex(e => e.OfferId, "OFFER_ID");

            entity.Property(e => e.CodeId).HasColumnName("CODE_ID");
            entity.Property(e => e.OfferId).HasColumnName("OFFER_ID");
            entity.Property(e => e.DiscId).HasColumnName("DISC_ID");
            entity.Property(e => e.Expiry)
                .HasColumnType("timestamp")
                .HasColumnName("EXPIRY");

            entity.HasOne(d => d.Disc).WithMany(p => p.Codes)
                .HasForeignKey(d => d.DiscId)
                .HasConstraintName("CODES_ibfk_2");

            entity.HasOne(d => d.Offer).WithMany(p => p.Codes)
                .HasForeignKey(d => d.OfferId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("CODES_ibfk_1");
        });

        modelBuilder.Entity<ContactInfo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("CONTACT_INFO");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Email)
                .HasMaxLength(128)
                .HasColumnName("EMAIL");
            entity.Property(e => e.Name)
                .HasMaxLength(128)
                .HasColumnName("NAME");
            entity.Property(e => e.Phone)
                .HasMaxLength(128)
                .HasColumnName("PHONE");
            entity.Property(e => e.Position)
                .HasMaxLength(128)
                .HasColumnName("POSITION");
        });

        modelBuilder.Entity<ContactRequest>(entity =>
        {
            entity.HasKey(e => e.ContReqId).HasName("PRIMARY");

            entity.ToTable("CONTACT_REQUESTS");

            entity.Property(e => e.ContReqId).HasColumnName("CONT_REQ_ID");
            entity.Property(e => e.Body)
                .HasMaxLength(1024)
                .HasColumnName("BODY");
            entity.Property(e => e.ContReqDate)
                .HasColumnType("timestamp")
                .HasColumnName("CONT_REQ_DATE");
            entity.Property(e => e.Subject)
                .HasMaxLength(64)
                .HasColumnName("SUBJECT");
        });

        modelBuilder.Entity<Discount>(entity =>
        {
            entity.HasKey(e => e.DiscId).HasName("PRIMARY");

            entity.ToTable("DISCOUNTS");

            entity.HasIndex(e => e.OfferId, "OFFER_ID");

            entity.Property(e => e.DiscId).HasColumnName("DISC_ID");
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
            entity.Property(e => e.Type)
                .HasMaxLength(128)
                .HasColumnName("TYPE");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Login).HasName("PRIMARY");

            entity.ToTable("REFRESH_TOKENS");

            entity.Property(e => e.Login)
                .HasMaxLength(128)
                .HasColumnName("LOGIN");
            entity.Property(e => e.Expiry)
                .HasColumnType("timestamp")
                .HasColumnName("EXPIRY");
            entity.Property(e => e.Token)
                .HasMaxLength(128)
                .HasColumnName("TOKEN");

            entity.HasOne(d => d.LoginNavigation).WithOne(p => p.RefreshToken)
                .HasForeignKey<RefreshToken>(d => d.Login)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("REFRESH_TOKENS_ibfk_1");
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.TransId).HasName("PRIMARY");

            entity.ToTable("TRANSACTIONS");

            entity.HasIndex(e => new { e.CodeId, e.OfferId }, "CODE_ID");

            entity.HasIndex(e => e.Login, "LOGIN");

            entity.HasIndex(e => e.Shop, "SHOP");

            entity.Property(e => e.TransId).HasColumnName("TRANS_ID");
            entity.Property(e => e.CodeId).HasColumnName("CODE_ID");
            entity.Property(e => e.Login)
                .HasMaxLength(128)
                .HasColumnName("LOGIN");
            entity.Property(e => e.OfferId).HasColumnName("OFFER_ID");
            entity.Property(e => e.Price).HasColumnName("PRICE");
            entity.Property(e => e.Shop).HasColumnName("SHOP");
            entity.Property(e => e.TransDate)
                .HasColumnType("timestamp")
                .HasColumnName("TRANS_DATE");

            entity.HasOne(d => d.LoginNavigation).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.Login)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("TRANSACTIONS_ibfk_2");

            entity.HasOne(d => d.ShopNavigation).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.Shop)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("TRANSACTIONS_ibfk_3");

            entity.HasOne(d => d.Code).WithMany(p => p.Transactions)
                .HasForeignKey(d => new { d.CodeId, d.OfferId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("TRANSACTIONS_ibfk_1");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Login).HasName("PRIMARY");

            entity.ToTable("USERS");

            entity.HasIndex(e => e.Organization, "ORGANIZATION");

            entity.Property(e => e.Login)
                .HasMaxLength(128)
                .HasColumnName("LOGIN");
            entity.Property(e => e.Credits).HasColumnName("CREDITS");
            entity.Property(e => e.Email)
                .HasMaxLength(128)
                .HasColumnName("EMAIL");
            entity.Property(e => e.LatestUpdate)
                .HasColumnType("timestamp")
                .HasColumnName("LATEST_UPDATE");
            entity.Property(e => e.Organization)
                .HasMaxLength(128)
                .HasColumnName("ORGANIZATION");
            entity.Property(e => e.Password)
                .HasMaxLength(128)
                .HasColumnName("PASSWORD");
            entity.Property(e => e.Salt)
                .HasMaxLength(128)
                .HasColumnName("SALT");
            entity.Property(e => e.Type)
                .HasMaxLength(128)
                .HasColumnName("TYPE");

            entity.HasOne(d => d.OrganizationNavigation).WithMany(p => p.Users)
                .HasForeignKey(d => d.Organization)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("USERS_ibfk_1");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
