using System.Text.Json;
using Prometej_core.Auth;
using Prometej_core.Models.Dtos;
using Prometej_core.Models.efModels;
using Prometej_core.Services.Contracts;
using Prometej_persistance;

namespace Prometej_api.Seed
{
    // A sample to show the app with: a Period Content for each Period and a few Public Quizzes,
    // from the files embedded under Seed/Content. It only fills what is empty: a Period that has
    // content keeps it, and the Quizzes are created only while their Creator has none. Off
    // unless "Seed:DemoContent" is set.
    public static class DemoContentSeeder
    {
        private const string PeriodFiles = "Prometej_api.Seed.Content.periods.";
        private const string QuizFiles = "Prometej_api.Seed.Content.quizzes.";

        public static void Run(IServiceProvider services, IConfiguration configuration)
        {
            if (!configuration.GetValue<bool>("Seed:DemoContent")) return;

            var db = services.GetRequiredService<DataContext>();

            var periodsWithContent = db.PeriodContents.Select(c => c.PeriodId).ToHashSet();
            foreach (var name in ResourceNames(PeriodFiles))
            {
                // A file is named after its Period: "8-barok.html" is the content of Period 8.
                var file = name[PeriodFiles.Length..];
                var periodId = int.Parse(file[..file.IndexOf('-')]);
                if (periodsWithContent.Contains(periodId)) continue;

                db.PeriodContents.Add(new PeriodContent { PeriodId = periodId, Content = Read(name) });
            }

            db.SaveChanges();

            var creator = db.Users.FirstOrDefault(u => u.Email == DemoCreator.Email);
            if (creator == null)
            {
                creator = new User
                {
                    FirstName = "Uredništvo",
                    LastName = "Prometeja",
                    Email = DemoCreator.Email,
                    // Not a bcrypt hash, so no password matches it: nobody signs in as this account.
                    PasswordHash = "!",
                    Role = Roles.Teacher,
                };
                db.Users.Add(creator);
                db.SaveChanges();
            }

            if (db.Quizzes.Any(q => q.CreatorId == creator.Id)) return;

            var quizService = services.GetRequiredService<IQuizService>();
            var json = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            // All or none: a Creator left with one Quiz would never get the others.
            using var transaction = db.Database.BeginTransaction();
            foreach (var name in ResourceNames(QuizFiles))
            {
                // A Quiz file is the body of a request to create that Quiz.
                var quiz = JsonSerializer.Deserialize<QuizCreateDto>(Read(name), json)!;
                quizService.Create(quiz.Quiz, quiz.Questions, creator.Id);
            }

            transaction.Commit();
        }

        private static IEnumerable<string> ResourceNames(string prefix) =>
            typeof(DemoContentSeeder).Assembly.GetManifestResourceNames()
                .Where(name => name.StartsWith(prefix, StringComparison.Ordinal))
                .Order(StringComparer.Ordinal);

        private static string Read(string resourceName)
        {
            using var stream = typeof(DemoContentSeeder).Assembly.GetManifestResourceStream(resourceName)!;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
