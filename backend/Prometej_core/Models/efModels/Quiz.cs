using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Prometej_core.Models.efModels
{
    public class Quiz
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public bool IsPrivate { get; set; }
        public int CreatorId { get; set; }
        public required User Creator { get; set; }
        public int? EntryCode { get; set; }
        public List<Question> Questions { get; set; }
        public List<SourceText> SourceTexts { get; set; }
        // The Period the Quiz is about. A Quiz that spans several, or was stored before
        // Quizzes had one, has none.
        public int? PeriodId { get; set; }
        public Period? Period { get; set; }
        // A test: sat once by each student, in a sitting, with no answer shown before its end.
        public bool IsTest { get; set; }
        // How long a sitting of this quiz may take. Null: as long as it takes.
        public int? TimeLimitMinutes { get; set; }
        // When a test stops taking sittings and opens its reviews. Null: not yet.
        public DateTime? ClosesAt { get; set; }

        // What the public list, the search and a Period's count of Quizzes all show.
        public static readonly Expression<Func<Quiz, bool>> IsListed =
            q => !q.IsPrivate && q.Questions.Any(x => !x.IsRetired);

        // A test is closed once its closing time has passed.
        public static bool IsClosed(DateTime? closesAt) => closesAt != null && closesAt <= DateTime.UtcNow;

    }
}
