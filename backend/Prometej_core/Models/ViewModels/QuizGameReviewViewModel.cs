namespace Prometej_core.Models.ViewModels
{
    // A quiz game as its player reads it again, with the quiz it belongs to as it is today.
    public class QuizGameReviewViewModel : QuizGameViewModel
    {
        public required string QuizTitle { get; set; }
        public int? PeriodId { get; set; }
        public string? PeriodName { get; set; }
        // Whether the public list still shows the quiz, so it can be played again from here.
        public bool QuizIsListed { get; set; }
        // The passages the answers were given beside, as they were then.
        public List<SourceTextViewModel> SourceTexts { get; set; } = [];
    }
}
