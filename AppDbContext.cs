using Microsoft.EntityFrameworkCore;
using RaceDay.API.Models;
using Route = RaceDay.API.Models.Route;
namespace RaceDay.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Enrolment> Enrolments { get; set; }
        public DbSet<Result> Results { get; set; }
        public DbSet<Route> Routes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Enforce unique constraints from SQL script
            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
            modelBuilder.Entity<Route>().HasIndex(r => r.EventID).IsUnique();
            modelBuilder.Entity<Result>().HasIndex(r => r.EnrolmentID).IsUnique();

            // Composite unique constraints
            modelBuilder.Entity<Category>().HasIndex(c => new { c.EventID, c.CategoryName }).IsUnique();
            modelBuilder.Entity<Enrolment>().HasIndex(e => new { e.EventID, e.ParticipantID }).IsUnique();
        }
    }
}