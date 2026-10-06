namespace Prometej_core.Models.ViewModels
{
    // How a quiz was played, for its creator: every quiz game, newest first, and the same
    // games counted by question and by player.
    public class QuizAnalyticsViewModel
    {
        public required string QuizTitle { get; set; }
        // What its creator needs to run a test: the code to hand out and when it closes.
        public bool IsTest { get; set; }
        public int? EntryCode { get; set; }
        public int? TimeLimitMinutes { get; set; }
        public DateTime? ClosesAt { get; set; }
        public bool IsClosed { get; set; }
        // Who has started a test, newest first. Empty for a quiz that is not a test.
        public required List<SittingRowViewModel> Sittings { get; set; }
        public required List<QuizGameViewModel> Games { get; set; }
        public required List<QuestionReportViewModel> Questions { get; set; }
        public required List<PlayerSummaryViewModel> Players { get; set; }
    }

    public class SittingRowViewModel
    {
        public const string Running = "running";

        public int Id { get; set; }
        public int UserId { get; set; }
        public required string UserName { get; set; }
        public DateTime StartedAt { get; set; }
        // "running", or the sitting's outcome: one of SittingOutcomes.
        public required string State { get; set; }
        // The result, once the sitting has ended.
        public int? QuizGameId { get; set; }
        public int? Score { get; set; }
        public int? MaxScore { get; set; }
    }

    public class QuestionReportViewModel
    {
        public int QuestionId { get; set; }
        // The title as it is today; the counts are over the answers as they were played.
        public required string QuestionTitle { get; set; }
        public required string Type { get; set; }
        public bool IsRetired { get; set; }
        // The question's source text as it is today, if it has one, and its caption.
        public int? SourceTextId { get; set; }
        public string? SourceTextCaption { get; set; }
        // Points possible and points won: a matching or an ordering question counts a row for
        // each of its pairs or places.
        public int AnswerCount { get; set; }
        public int CorrectCount { get; set; }
        // The wrong answer chosen most often, as its text was when played. None if nobody was
        // wrong, and none for a question of several points: its lines say it for each.
        public string? MostChosenWrongAnswer { get; set; }
        public int MostChosenWrongCount { get; set; }
        // A line for each left-hand item of a matching question or each place of an ordering
        // one, as they were played. Empty for a choice question.
        public required List<QuestionLineViewModel> Lines { get; set; }
    }

    public class QuestionLineViewModel
    {
        // The left-hand item, or the number of the place.
        public required string Label { get; set; }
        public int AnswerCount { get; set; }
        public int CorrectCount { get; set; }
        public string? MostChosenWrongAnswer { get; set; }
        public int MostChosenWrongCount { get; set; }
    }

    public class PlayerSummaryViewModel
    {
        public int UserId { get; set; }
        public required string UserName { get; set; }
        public int GameCount { get; set; }
        // A score goes with the points its game could give: the quiz may have been edited
        // between two plays.
        public int FirstScore { get; set; }
        public int FirstMaxScore { get; set; }
        // The game with the highest share of points won.
        public int BestScore { get; set; }
        public int BestMaxScore { get; set; }
        public DateTime LastPlayed { get; set; }
    }
}
