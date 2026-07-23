using GelirGiderPanel.Enums;
using GelirGiderPanel.Initializer;
using GelirGiderPanel.Models;
using Microsoft.EntityFrameworkCore;

namespace GelirGiderPanel.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Transaction> Transactions => Set<Transaction>();
        public DbSet<TransactionType> TransactionTypes => Set<TransactionType>();

        public DbSet<AppUser> Users => Set<AppUser>();

        public DbSet<CariAccount> CariAccounts { get; set; }
        public DbSet<CariTransaction> CariTransactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ---- İlişki yapılandırmaları ----
            // Kategori silinirse altındaki işlemler silinmesin (Restrict):
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Category)
                .WithMany(c => c.Transactions)
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.TransactionType)
                .WithMany(tt => tt.Transactions)
                .HasForeignKey(t => t.TransactionTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Sık kullanılacak sorgular için index:
            modelBuilder.Entity<Transaction>()
                .HasIndex(t => t.Date);

            // ---- Seed Data ----
            // İşlem türleri sabittir; Id'leri kodda güvenle kullanılabilir.
            //modelBuilder.Entity<TransactionType>().HasData(
            //    new TransactionType { Id = 1, Name = "Gelir" },
            //    new TransactionType { Id = 2, Name = "Gider" }
            //);

            modelBuilder.Entity<TransactionType>().HasData(
                Enum.GetValues<TransactionStatus>().Select(e=>new TransactionType { Id=(int)e,Name=e.ToString()})
                );

            
            DataInitializer.Seed(modelBuilder);
            var seedDate = new DateTime(2026, 1, 1);
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Mutfak",   Description = "Mutfak ve malzeme giderleri", IsActive = true, CreatedAt = seedDate },
                new Category { Id = 2, Name = "Satış",    Description = "Ürün satış gelirleri",        IsActive = true, CreatedAt = seedDate },
                new Category { Id = 3, Name = "Kira",     Description = "İşyeri kira ödemeleri",       IsActive = true, CreatedAt = seedDate },
                new Category { Id = 4, Name = "Personel", Description = "Maaş ve personel giderleri",  IsActive = true, CreatedAt = seedDate }
            );

            modelBuilder.Entity<CariAccount>(entity =>
            {
                entity.Property(a => a.OpeningBalance).HasColumnType("decimal(18,2)");
                entity.HasIndex(a => a.Name);
            });

            modelBuilder.Entity<CariTransaction>(entity =>
            {
                entity.Property(t => t.DebitAmount).HasColumnType("decimal(18,2)");
                entity.Property(t => t.CreditAmount).HasColumnType("decimal(18,2)");
                entity.Property(t => t.UnitPrice).HasColumnType("decimal(18,2)");
                entity.HasOne(t => t.CariAccount)
                      .WithMany(a => a.Transactions)
                      .HasForeignKey(t => t.CariAccountId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(t => t.Date);
                entity.HasIndex(t => t.CariAccountId);
            });
        }
    }
}
