using Microsoft.EntityFrameworkCore;
using Prometej_core.Models.efModels;

namespace Prometej_persistance
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options){}

        public DbSet<User> Users { get; set; }
        public DbSet<Period> Periods { get; set; }
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
            // The Periods are a fixed list the code depends on, so the migrations insert them.
            modelBuilder.Entity<Period>(period =>
            {
                period.Property(p => p.Id).ValueGeneratedNever();
                period.HasData(CurriculumPeriods.All);
            });
            // A Period has at most one content: the foreign key carries a unique index.
            modelBuilder.Entity<PeriodContent>()
                .HasOne(c => c.Period).WithOne().HasForeignKey<PeriodContent>(c => c.PeriodId);
        }

    }
}
