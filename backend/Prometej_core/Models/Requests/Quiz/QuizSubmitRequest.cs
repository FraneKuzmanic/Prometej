using System.ComponentModel.DataAnnotations;

namespace Prometej_core.Models.Requests.Quiz
{
    // Only what the server cannot know. The player, the score, the time and each correct
    // answer are taken from the session, the stored questions and the server's clock.
    public class QuizSubmitRequest
    {
        public int QuizId { get; set; }
        [Required, MinLength(1), NoNullItems]
        public required List<AnswerSubmitRequest> Answers { get; set; }
        // Made by the client for one play. A submit that repeats it gets the stored quiz game back.
        public Guid? SubmissionKey { get; set; }
    }

    // One question's answer, as numbers. Which of the three it carries depends on the
    // question's type; the texts are read from the stored question.
    public class AnswerSubmitRequest : IValidatableObject
    {
        public int QuestionId { get; set; }
        // Choice: which of the question's four answers was chosen.
        [Range(1, 4)]
        public int? ChosenOption { get; set; }
        // Matching: for each pair in order, the number of the right-hand option chosen for its
        // left item. The pairs' own right-hand texts are numbered first, then the extras.
        [MaxLength(5)]
        public List<int>? Matches { get; set; }
        // Ordering: for each place in order, the number of the item put there.
        [MaxLength(6)]
        public List<int>? Order { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var given = (ChosenOption != null ? 1 : 0) + (Matches != null ? 1 : 0) + (Order != null ? 1 : 0);
            if (given != 1)
            {
                yield return new ValidationResult(
                    "An answer has exactly one of ChosenOption, Matches and Order.", [nameof(ChosenOption)]);
            }
        }
    }
}
