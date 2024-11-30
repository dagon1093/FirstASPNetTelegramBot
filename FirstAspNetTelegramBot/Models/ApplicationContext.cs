using Microsoft.EntityFrameworkCore;

namespace FirstAspNetTelegramBot.Models
{
    public class ApplicationContext : DbContext
    {
        public DbSet<Note> Notes { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;

        public ApplicationContext()
        {
            Database.EnsureCreated();
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=NotesDb;Username=postgres;Password=Rp_9i7g7");
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Note>()
                .HasOne(u => u.User)
                .WithMany(c => c.Notes)
                .HasForeignKey(u => u.UserId);
        }
    }
}
