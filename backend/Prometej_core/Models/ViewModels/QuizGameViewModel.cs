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
    }
}
