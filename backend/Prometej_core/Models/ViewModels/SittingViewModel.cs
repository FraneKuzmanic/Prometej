namespace Prometej_core.Models.ViewModels
{
    // A running sitting as its user works on it: the questions without any answer, and what
    // was saved so far.
    public class SittingViewModel
    {
        public int Id { get; set; }
        public int QuizId { get; set; }
        public required string QuizTitle { get; set; }
        public bool IsTest { get; set; }
        public DateTime StartedAt { get; set; }
        // When the server ends it. Null without a time limit or a closing time.
        public DateTime? EndsAt { get; set; }
        // The server's clock, so the browser can count down against it and not its own.
        public DateTime ServerNow { get; set; }
        public required List<SittingQuestionViewModel> Questions { get; set; }
        public required List<SourceTextViewModel> SourceTexts { get; set; }
    }

    // A question as a sitting shows it. Filled member by member and never mapped from a
    // Question, so nothing added to a question later can reach a test by itself.
    public class SittingQuestionViewModel
    {
        public int QuestionId { get; set; }
        public required string QuestionTitle { get; set; }
        public required string Type { get; set; }
        // Choice: the four answers, in their stored order.
        public List<string>? Options { get; set; }
        // Matching: the left-hand items in order, and the right-hand options as shuffled.
        public List<string>? Lefts { get; set; }
        public List<string>? Rights { get; set; }
        // Ordering: the items as shuffled.
        public List<string>? Items { get; set; }
        public int? SourceTextId { get; set; }
        // What was saved for it so far, in the shown numbering.
        public int[]? Given { get; set; }
    }
}
