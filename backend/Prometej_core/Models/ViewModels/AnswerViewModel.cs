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
        public int Position { get; set; }
        public int? SourceTextId { get; set; }
    }
}
