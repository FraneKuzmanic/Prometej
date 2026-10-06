namespace Prometej_core.Models.efModels
{
    // One user's go at a quiz under test conditions: started on the server, which keeps its
    // answers and its clock. When it ends it writes a quiz game and stays as the record.
    public class Sitting
    {
        public int Id { get; set; }
        public int QuizId { get; set; }
        public Quiz Quiz { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public DateTime StartedAt { get; set; }
        // Where the quiz's time limit ends it, fixed at the start. Null without a limit.
        public DateTime? TimeLimitEndsAt { get; set; }
        // Null while it runs.
        public DateTime? FinishedAt { get; set; }
        // One of SittingOutcomes, null while it runs.
        public string? Outcome { get; set; }
        // The result, once it has ended.
        public int? QuizGameId { get; set; }
        public QuizGame? QuizGame { get; set; }
        // One per question while it runs; deleted when it ends.
        public List<SittingAnswer> Answers { get; set; } = [];
        // Changes with every write, so two requests cannot both end the same sitting.
        public uint Version { get; set; }
    }

    public class SittingAnswer
    {
        public int Id { get; set; }
        public int SittingId { get; set; }
        public Sitting Sitting { get; set; }
        public int QuestionId { get; set; }
        public Question Question { get; set; }
        // The question's place in the sitting.
        public int Position { get; set; }
        // Matching and ordering: the stored number (from 1) of the right-hand option or item
        // shown at each place of the list the user sees. Null for a choice question.
        public int[]? Shown { get; set; }
        // What the user gave, as numbers in the shown order, 0 for a point left empty.
        // Null until the question is first answered.
        public int[]? Given { get; set; }
    }

    public static class SittingOutcomes
    {
        public const string Submitted = "submitted";
        // The time limit or the test's closing ended it.
        public const string Expired = "expired";
    }
}
