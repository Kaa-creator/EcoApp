using Microsoft.EntityFrameworkCore;
using EcoApp.API.Models;

namespace EcoApp.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<EcoPoint> EcoPoints { get; set; }
        public DbSet<EcoTask> EcoTasks { get; set; }
        public DbSet<TaskReport> TaskReports { get; set; }
        public DbSet<Article> Articles { get; set; }

        public DbSet<EcoEvent> EcoEvents { get; set; }
        public DbSet<EmailConfirmation> EmailConfirmations { get; set; }
        public DbSet<TelegramSubscription> TelegramSubscriptions { get; set; }
        public DbSet<NotificationLog> NotificationLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<EmailConfirmation>()
                .HasIndex(e => e.Token)
                .IsUnique();

            modelBuilder.Entity<TelegramSubscription>()
                .HasIndex(t => new { t.UserId, t.ChatId })
                .IsUnique();
        }
    }
}