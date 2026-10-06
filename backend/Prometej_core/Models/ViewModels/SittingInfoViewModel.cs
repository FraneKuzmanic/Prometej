namespace Prometej_core.Models.ViewModels
{
    // What the start screen shows of a quiz that can be sat, and where the caller stands with it.
    public class SittingInfoViewModel
    {
        public int QuizId { get; set; }
        public required string Title { get; set; }
        public bool IsTest { get; set; }
        public int QuestionCount { get; set; }
        public int MaxScore { get; set; }
        public int? TimeLimitMinutes { get; set; }
        public DateTime? ClosesAt { get; set; }
        public bool IsClosed { get; set; }
        // The caller made the quiz, and so cannot sit it.
        public bool IsOwn { get; set; }
        // The caller has a sitting of it running.
        public bool Running { get; set; }
        // The caller's sitting of a test, once it has ended.
        public SittingResultViewModel? Result { get; set; }
    }

    // How a sitting ended. It never carries the answers: those are the review's.
    public class SittingResultViewModel
    {
        public int GameId { get; set; }
        public int Score { get; set; }
        public int MaxScore { get; set; }
        // One of SittingOutcomes.
        public required string Outcome { get; set; }
        // False for a test that is not closed yet.
        public bool ReviewAvailable { get; set; }
    }
}
