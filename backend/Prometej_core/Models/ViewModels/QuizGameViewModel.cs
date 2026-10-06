namespace Prometej_core.Models.ViewModels
{
    public class QuizGameViewModel
    {
        public int Id { get; set; }
        public int QuizId { get; set; }
        public int UserId { get; set; }
        public required string UserName { get; set; }
        public int Score { get; set; }
        public DateTime DatePlayed { get; set; }
        public required List<AnswerViewModel> Answers { get; set; }
        // One of PlayedAs.
        public string PlayedAs { get; set; } = ViewModels.PlayedAs.Practice;
    }

    // The three ways a quiz game comes about.
    public static class PlayedAs
    {
        // Answered and marked question by question, submitted at the end.
        public const string Practice = "practice";
        // A sitting of a public quiz, chosen by the player.
        public const string Mock = "mock";
        // A sitting of a test.
        public const string Test = "test";

        public static string Of(bool quizIsTest, bool hasSitting) =>
            quizIsTest ? Test : hasSitting ? Mock : Practice;
    }
}
