using System.ComponentModel.DataAnnotations;

namespace Prometej_core.Models.Requests.Quiz
{
    // Only what the server cannot know. The player, the score, the time and each correct
    // answer are taken from the session, the stored questions and the server's clock.
    public class QuizSubmitRequest
    {
        public int QuizId { get; set; }
        [Required, MinLength(1)]
        public required List<AnswerSubmitRequest> Answers { get; set; }
    }

    public class AnswerSubmitRequest
    {
        public int QuestionId { get; set; }
        // Which of the question's four answers was chosen.
        [Range(1, 4)]
        public int ChosenOption { get; set; }
    }
}
