namespace Prometej_core.Models.ViewModels
{
    // What a player sees of their own plays: every quiz game, newest first, and above it
    // their progress in each Period they have practised.
    public class MyQuizGamesViewModel
    {
        public required List<PeriodProgressViewModel> Progress { get; set; }
        public required List<PlayedQuizGameViewModel> Games { get; set; }
    }

    public class PlayedQuizGameViewModel
    {
        public int Id { get; set; }
        public int QuizId { get; set; }
        public required string QuizTitle { get; set; }
        public string? PeriodName { get; set; }
        public int Score { get; set; }
        // The points the game could give when it was played, not what the quiz gives now.
        public int MaxScore { get; set; }
        public DateTime DatePlayed { get; set; }
    }

    public class PeriodProgressViewModel
    {
        public int PeriodId { get; set; }
        public required string PeriodName { get; set; }
        // How many quizzes the public list shows for the Period.
        public int QuizCount { get; set; }
        // How many of those the player has played.
        public int PlayedCount { get; set; }
        // The player's best play of each played quiz, averaged.
        public int AverageBestPercent { get; set; }
    }
}
