using LanguageLearningApp.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearningApp.Api.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Message> Messages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // İlişkileri veya tablo adlarını burada özelleştirebilirsiniz
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(u => u.Id);

                entity.HasMany(u => u.Messages)
                      .WithOne(m => m.User)
                      .HasForeignKey(m => m.UserId);
            });

            modelBuilder.Entity<Message>(entity =>
            {
                entity.ToTable("Messages");
                entity.HasKey(m => m.Id);
            });
        }
    }
}
