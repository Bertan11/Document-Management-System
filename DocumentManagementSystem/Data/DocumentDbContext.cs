using DocumentManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace DocumentManagementSystem.Data
{
    public class DocumentDbContext : DbContext
    {
        public DocumentDbContext(DbContextOptions<DocumentDbContext> options)
            : base(options)
        {
        }

        public DbSet<Document> Documents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Document>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Guid wird in der App gesetzt, nicht von der DB generiert
                entity.Property(e => e.Id)
                      .ValueGeneratedNever();

                entity.Property(e => e.Title)
                      .IsRequired();

                entity.Property(e => e.Content);

                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("NOW()");
            });
        }
    }
}
