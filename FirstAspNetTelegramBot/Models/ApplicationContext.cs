using Microsoft.EntityFrameworkCore;

namespace FirstAspNetTelegramBot.Models
{
    public class ApplicationContext : DbContext
    {
        public DbSet<Note> Notes { get; set; } = null!;

        public ApplicationContext()
        {
            Database.EnsureCreated();
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=NotesDb;Username=postgres;Password=Rp_9i7g7");
        }
    }
}
