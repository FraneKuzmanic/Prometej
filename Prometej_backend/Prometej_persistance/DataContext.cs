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

    }
}
