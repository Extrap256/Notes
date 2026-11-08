using Microsoft.EntityFrameworkCore;
using Notes.Infrastructure.Data.Entities;

namespace Notes.Infrastructure.Data
{
    public class NotesServerDBContext : DbContext
    {
        public DbSet<NoteStorageEntity> Notes { get; set; } = null!;
        public DbSet<UserStorageEntity> Users { get; set; } = null!;
        public DbSet<RefreshTokenStorageEntity> RefreshTokens { get; set; } = null!;

        public NotesServerDBContext(DbContextOptions<NotesServerDBContext> contextOptions)
            : base(contextOptions)
        { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserStorageEntity>(user =>
            {
                user.HasKey(u => u.Id);

                user.HasAlternateKey(u => u.Login);

                user.HasIndex(u => u.Login);
            });

            modelBuilder.Entity<NoteStorageEntity>(note =>
            {
                note.HasKey(n => n.Id);
                note.Property(n => n.Title)
                    .HasMaxLength(200);

                note.HasOne<UserStorageEntity>()
                    .WithMany()
                    .HasForeignKey(n => n.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                note.HasIndex(n => n.UserId);
            });


            modelBuilder.Entity<RefreshTokenStorageEntity>(token =>
            {
                token.HasKey(t => t.Id);

                token.HasAlternateKey(t => t.Hash);

                token.HasOne<UserStorageEntity>()
                    .WithMany()
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
