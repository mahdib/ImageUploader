using ImageUploader.Models;
using Microsoft.EntityFrameworkCore;

namespace ImageUploader.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        { }

        public DbSet<Photo> Photos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<Photo>();

            entity.ToTable("Images");

            entity.HasKey(p => p.Id);

            entity.Property(p => p.FileName)
            .HasMaxLength(250)
            .IsUnicode()
            .IsRequired();

            entity.Property(p => p.Url)
            .HasMaxLength(2048)
            .IsUnicode()
            .IsRequired();
        }
    }
}