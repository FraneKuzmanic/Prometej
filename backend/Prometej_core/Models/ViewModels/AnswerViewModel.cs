namespace Prometej_core.Models.ViewModels
{
    public class AnswerViewModel
    {
        public int Id { get; set; }
        public int QuizGameId { get; set; }
        public int QuestionId { get; set; }
        public required string QuestionTitle { get; set; }
        public string? ExploreMore { get; set; }
        public required string AnswerText { get; set; }
        public required string CorrectAnswer { get; set; }
        // The left-hand item of a matching question, or the place of an ordering one, this
        // row is about. A row with neither is a choice question's.
        public string? Item { get; set; }
        public int? Place { get; set; }
        public int Position { get; set; }
        public int? SourceTextId { get; set; }
    }
}
