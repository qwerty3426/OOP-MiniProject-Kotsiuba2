using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace shelf.Models
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Book> Books { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Note> Notes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Початкові дані для категорій
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Фентезі" },
                new Category { Id = 2, Name = "Детектив" },
                new Category { Id = 3, Name = "Роман" }
            );
        }
    }
}