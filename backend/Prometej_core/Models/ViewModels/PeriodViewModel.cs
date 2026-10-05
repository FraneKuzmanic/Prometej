namespace Prometej_core.Models.ViewModels
{
    public class PeriodViewModel
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string TimeFrame { get; set; }
        public required string Description { get; set; }
        public string? Image { get; set; }
        // The Public Quizzes about this Period that have Questions.
        public int QuizCount { get; set; }
        // The topics in this Period's discussion.
        public int TopicCount { get; set; }
    }
}
