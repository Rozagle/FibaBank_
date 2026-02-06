using FibaPlus_Bank.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace FibaPlus_Bank.Models
{
    public partial class FibraPlusBankDbContext : DbContext
    {
        public FibraPlusBankDbContext(DbContextOptions<FibraPlusBankDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Account> Accounts { get; set; }
        public virtual DbSet<Card> Cards { get; set; }
        public virtual DbSet<Investment> Investments { get; set; }
        public virtual DbSet<Transaction> Transactions { get; set; }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<Institution> Institutions { get; set; }
        public virtual DbSet<InvestmentTransaction> InvestmentTransactions { get; set; }
        public virtual DbSet<PaymentType> PaymentTypes { get; set; }
        public virtual DbSet<AccountProduct> AccountProducts { get; set; }
        public DbSet<Currency> Currencies { get; set; }
        public virtual DbSet<SystemSetting> SystemSettings { get; set; }
        public virtual DbSet<SystemLog> SystemLogs { get; set; }
        public DbSet<InterestTier> InterestTiers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<Account>(entity =>
            {
                entity.HasKey(e => e.AccountId);
                entity.HasIndex(e => e.Iban).IsUnique();
                entity.HasIndex(e => e.AccountNumber).IsUnique();
                entity.Property(e => e.AccountName).HasMaxLength(50);
                entity.Property(e => e.AccountNumber).HasMaxLength(20);

                entity.Property(e => e.Balance).HasDefaultValue(0m).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.InterestRate).HasColumnType("decimal(18, 4)");

                entity.Property(e => e.CurrencyCode).HasMaxLength(5).HasDefaultValue("TRY");
                entity.Property(e => e.Iban).HasMaxLength(30).HasColumnName("IBAN");
                entity.HasOne(d => d.User).WithMany(p => p.Accounts).HasForeignKey(d => d.UserId).OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<InterestTier>(entity =>
            {
                entity.Property(e => e.MinAmount).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.MaxAmount).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.InterestRate).HasColumnType("decimal(18, 4)");
            });

            modelBuilder.Entity<InvestmentTransaction>(entity =>
            {
                entity.Property(e => e.Quantity).HasColumnType("decimal(18, 4)");
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 4)");
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");
            });

            modelBuilder.Entity<Card>(entity =>
            {
                entity.HasKey(e => e.CardId);
                entity.HasIndex(e => e.CardNumber).IsUnique();
                entity.Property(e => e.CardLimit).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.CardNumber).HasMaxLength(16);
                entity.Property(e => e.CardType).HasMaxLength(20);
                entity.Property(e => e.Cvv).HasMaxLength(3).HasColumnName("CVV");
                entity.Property(e => e.Debt).HasDefaultValue(0m).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.ExpiryDate).HasMaxLength(5);
                entity.Property(e => e.IsInternetEnabled).HasDefaultValue(true);
                entity.HasOne(d => d.Account).WithMany(p => p.Cards).HasForeignKey(d => d.AccountId).OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<Investment>(entity =>
            {
                entity.HasKey(e => e.InvestmentId);
                entity.Property(e => e.Quantity).HasColumnType("decimal(18, 4)");
                entity.Property(e => e.PurchasePrice).HasColumnType("decimal(18, 4)");
                entity.Property(e => e.CurrentPrice).HasColumnType("decimal(18, 4)");
                entity.Property(e => e.InstrumentCode).HasMaxLength(20).IsRequired();
                entity.Property(e => e.InstrumentName).HasMaxLength(150).IsRequired();
                entity.Property(e => e.InvestmentType).HasMaxLength(50).IsRequired();
                entity.Property(e => e.InstrumentType).HasMaxLength(50).IsRequired();
                entity.Property(e => e.CurrencyCode).HasMaxLength(5).HasDefaultValue("TRY");
            });

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasKey(e => e.TransactionId);
                entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.CategoryIcon).HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(200);
                entity.Property(e => e.TransactionDate).HasDefaultValueSql("GETDATE()").HasColumnType("datetime");
                entity.Property(e => e.TransactionType).HasMaxLength(20);
                entity.Property(e => e.SenderIBAN).HasMaxLength(30);
                entity.Property(e => e.ReceiverIBAN).HasMaxLength(30);
                entity.Property(e => e.ReceiverName).HasMaxLength(100);
                entity.Property(e => e.TransactionStatus).HasMaxLength(20);
                entity.Property(e => e.ReferenceCode).HasMaxLength(20);
                entity.HasOne(d => d.Account).WithMany(p => p.Transactions).HasForeignKey(d => d.AccountId).OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserId);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()").HasColumnType("datetime");
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.FullName).HasMaxLength(100);
                entity.Property(e => e.IdentityNumber).HasMaxLength(11);
                entity.Property(e => e.PasswordHash).HasMaxLength(255);
                entity.Property(e => e.Role).HasMaxLength(20);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}