using Microsoft.EntityFrameworkCore;
using Prometej_core.Models.efModels;

namespace Prometej_persistance
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options){}

        public DbSet<User> Users { get; set; }
        public DbSet<PeriodContent> PeriodContents { get; set; }
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Answer> Answers { get; set; }
        public DbSet<QuizGame> QuizGames { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Quiz search compares text without its diacritics.
            modelBuilder.HasPostgresExtension("unaccent");
            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
            // Only a private quiz has an entry code; PostgreSQL lets any number of rows hold NULL.
            modelBuilder.Entity<Quiz>().HasIndex(q => q.EntryCode).IsUnique();
        }

    }
}
