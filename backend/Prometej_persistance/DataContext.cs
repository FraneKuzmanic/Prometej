using System.Text.Json;
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
        public DbSet<SourceText> SourceTexts { get; set; }
        public DbSet<Topic> Topics { get; set; }
        public DbSet<Reply> Replies { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Quiz search compares text without its diacritics.
            modelBuilder.HasPostgresExtension("unaccent");
            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
            // Only a private quiz has an entry code; PostgreSQL lets any number of rows hold NULL.
            modelBuilder.Entity<Quiz>().HasIndex(q => q.EntryCode).IsUnique();
            // One quiz game per player and submission key; PostgreSQL lets any number of rows hold no key.
            modelBuilder.Entity<QuizGame>().HasIndex(g => new { g.UserId, g.SubmissionKey }).IsUnique();
            // The Periods are a fixed list the code depends on, so the migrations insert them.
            modelBuilder.Entity<Period>(period =>
            {
                period.Property(p => p.Id).ValueGeneratedNever();
                period.HasData(CurriculumPeriods.All);
            });
            // A Period has at most one content: the foreign key carries a unique index.
            modelBuilder.Entity<PeriodContent>()
                .HasOne(c => c.Period).WithOne().HasForeignKey<PeriodContent>(c => c.PeriodId);
            // Deleting a source text never takes a question or an answer along. One that was
            // played is retired instead, so an answer loses its link only when its quiz goes.
            modelBuilder.Entity<Question>()
                .HasOne(q => q.SourceText).WithMany().HasForeignKey(q => q.SourceTextId).OnDelete(DeleteBehavior.SetNull);
            // Questions stored before there were types are choice questions.
            modelBuilder.Entity<Question>().Property(q => q.Type).HasMaxLength(20).HasDefaultValue(QuestionTypes.Choice);
            // Replaced whole on every edit and compared by reference, so it is never changed in place.
            modelBuilder.Entity<Question>().Property(q => q.Content).HasColumnType("jsonb").HasConversion(
                content => content == null ? null : JsonSerializer.Serialize(content, QuestionContent.Json),
                json => json == null ? null : JsonSerializer.Deserialize<QuestionContent>(json, QuestionContent.Json));
            modelBuilder.Entity<Answer>()
                .HasOne<SourceText>().WithMany().HasForeignKey(a => a.SourceTextId).OnDelete(DeleteBehavior.SetNull);
            // A post outlives its author's account: the row stays and names nobody.
            modelBuilder.Entity<Topic>()
                .HasOne(t => t.Author).WithMany().HasForeignKey(t => t.AuthorId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Reply>()
                .HasOne(r => r.Author).WithMany().HasForeignKey(r => r.AuthorId).OnDelete(DeleteBehavior.SetNull);
        }

    }
}
