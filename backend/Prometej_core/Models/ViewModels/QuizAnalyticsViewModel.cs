namespace Prometej_core.Models.ViewModels
{
    // How a quiz was played, for its creator: every quiz game, newest first, and the same
    // games counted by question and by player.
    public class QuizAnalyticsViewModel
    {
        public required string QuizTitle { get; set; }
        public required List<QuizGameViewModel> Games { get; set; }
        public required List<QuestionReportViewModel> Questions { get; set; }
        public required List<PlayerSummaryViewModel> Players { get; set; }
    }

    public class QuestionReportViewModel
    {
        public int QuestionId { get; set; }
        // The title as it is today; the counts are over the answers as they were played.
        public required string QuestionTitle { get; set; }
        public bool IsRetired { get; set; }
        // The caption of the question's source text as it is today, if it has one.
        public string? SourceTextCaption { get; set; }
        public int AnswerCount { get; set; }
        public int CorrectCount { get; set; }
        // The wrong answer chosen most often, as its text was when played. None if nobody was wrong.
        public string? MostChosenWrongAnswer { get; set; }
        public int MostChosenWrongCount { get; set; }
    }

    public class PlayerSummaryViewModel
    {
        public int UserId { get; set; }
        public required string UserName { get; set; }
        public int GameCount { get; set; }
        // A score goes with the number of questions its game was played with: the quiz may
        // have been edited between two plays.
        public int FirstScore { get; set; }
        public int FirstQuestionCount { get; set; }
        // The game with the highest share of correct answers.
        public int BestScore { get; set; }
        public int BestQuestionCount { get; set; }
        public DateTime LastPlayed { get; set; }
    }
}
