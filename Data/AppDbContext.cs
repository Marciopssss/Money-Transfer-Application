// File: Data/AppDbContext.cs
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SwiftPay.Models;

namespace SwiftPay.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // ── DbSets ──────────────────────────────────────────────
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Beneficiary> Beneficiaries { get; set; }
        public DbSet<TopUp> TopUps { get; set; }
        public DbSet<Agent> Agents { get; set; }
        public DbSet<CommissionRate> CommissionRates { get; set; }
        public DbSet<Currency> Currencies { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ── Account ─────────────────────────────────────────
            builder.Entity<Account>(e =>
            {
                e.HasIndex(a => a.SerialNumber).IsUnique();
                e.Property(a => a.Balance).HasColumnType("decimal(18,2)");
                e.HasOne(a => a.User)
                 .WithMany(u => u.Accounts)
                 .HasForeignKey(a => a.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── Transaction ──────────────────────────────────────
            builder.Entity<Transaction>(e =>
            {
                e.HasIndex(t => t.SerialNumber).IsUnique();
                e.Property(t => t.Amount).HasColumnType("decimal(18,2)");
                e.Property(t => t.ConvertedAmount).HasColumnType("decimal(18,2)");
                e.Property(t => t.ExchangeRate).HasColumnType("decimal(18,6)");
                e.Property(t => t.CommissionAmount).HasColumnType("decimal(18,2)");

                // Sender → no cascade (avoid multiple cascade paths)
                e.HasOne(t => t.SenderAccount)
                 .WithMany(a => a.SentTransactions)
                 .HasForeignKey(t => t.SenderAccountId)
                 .OnDelete(DeleteBehavior.Restrict);

                // Receiver → no cascade
                e.HasOne(t => t.ReceiverAccount)
                 .WithMany(a => a.ReceivedTransactions)
                 .HasForeignKey(t => t.ReceiverAccountId)
                 .OnDelete(DeleteBehavior.Restrict);

                // Beneficiary → optional
                e.HasOne(t => t.Beneficiary)
                 .WithMany(b => b.Transactions)
                 .HasForeignKey(t => t.BeneficiaryId)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            // ── Beneficiary ──────────────────────────────────────
            builder.Entity<Beneficiary>(e =>
            {
                e.HasOne(b => b.User)
                 .WithMany(u => u.Beneficiaries)
                 .HasForeignKey(b => b.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── TopUp ────────────────────────────────────────────
            builder.Entity<TopUp>(e =>
            {
                e.Property(t => t.Amount).HasColumnType("decimal(18,2)");
                e.HasOne(t => t.Account)
                 .WithMany(a => a.TopUps)
                 .HasForeignKey(t => t.AccountId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── Agent ────────────────────────────────────────────
            builder.Entity<Agent>(e =>
            {
                e.HasOne(a => a.User)
                 .WithOne()
                 .HasForeignKey<Agent>(a => a.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── CommissionRate ───────────────────────────────────
            builder.Entity<CommissionRate>(e =>
            {
                e.Property(c => c.Rate).HasColumnType("decimal(5,2)");
                e.HasIndex(c => new { c.FromCurrency, c.ToCurrency }).IsUnique();
            });

            // ── Review ───────────────────────────────────────────
            builder.Entity<Review>(e =>
            {
                e.HasOne(r => r.User)
                 .WithMany(u => u.Reviews)
                 .HasForeignKey(r => r.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── Notification ─────────────────────────────────────
            builder.Entity<Notification>(e =>
            {
                e.HasOne(n => n.User)
                 .WithMany(u => u.Notifications)
                 .HasForeignKey(n => n.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── Seed default currencies ──────────────────────────
            builder.Entity<Currency>().HasData(
                new Currency { Id = 1, Code = "USD", Name = "US Dollar", Symbol = "$", Flag = "🇺🇸", IsActive = true },
                new Currency { Id = 2, Code = "EUR", Name = "Euro", Symbol = "€", Flag = "🇪🇺", IsActive = true },
                new Currency { Id = 3, Code = "GBP", Name = "British Pound", Symbol = "£", Flag = "🇬🇧", IsActive = true },
                new Currency { Id = 4, Code = "LBP", Name = "Lebanese Pound", Symbol = "LL", Flag = "🇱🇧", IsActive = true },
                new Currency { Id = 5, Code = "AED", Name = "UAE Dirham", Symbol = "د.إ", Flag = "🇦🇪", IsActive = true },
                new Currency { Id = 6, Code = "SAR", Name = "Saudi Riyal", Symbol = "﷼", Flag = "🇸🇦", IsActive = true }
            );

            // ── Seed default commission rates ────────────────────────────────────
            builder.Entity<CommissionRate>().HasData(
                new CommissionRate { Id = 1, FromCurrency = "USD", ToCurrency = "EUR", Rate = 1.8m, UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new CommissionRate { Id = 2, FromCurrency = "USD", ToCurrency = "LBP", Rate = 2.2m, UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new CommissionRate { Id = 3, FromCurrency = "EUR", ToCurrency = "GBP", Rate = 1.5m, UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new CommissionRate { Id = 4, FromCurrency = "AED", ToCurrency = "USD", Rate = 1.9m, UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new CommissionRate { Id = 5, FromCurrency = "SAR", ToCurrency = "EUR", Rate = 2.0m, UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );
        }
    }
}
