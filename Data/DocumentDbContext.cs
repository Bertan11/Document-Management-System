using Microsoft.EntityFrameworkCore;
using DocumentManagementSystem.Models;

namespace DocumentManagementSystem.Data
{
    public class DocumentDbContext : DbContext
    {
        public DocumentDbContext(DbContextOptions<DocumentDbContext> options) : base(options)
        {
        }

        public DbSet<Document> Documents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Document>(entity =>
            {
                entity.HasKey(d => d.Id);
                entity.Property(d => d.Id).ValueGeneratedOnAdd();
                entity.Property(d => d.Filename).IsRequired().HasMaxLength(255);
                entity.Property(d => d.ContentType).HasMaxLength(50);
                entity.Property(d => d.FilePath).HasMaxLength(500);
                entity.Property(d => d.Tags).HasMaxLength(100);

                entity.HasIndex(d => d.Filename);
                entity.HasIndex(d => d.UploadDate);
                entity.HasIndex(d => d.IsProcessed);
            });
        }
    }
}