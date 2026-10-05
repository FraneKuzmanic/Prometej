using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prometej_core.Models.efModels
{
    public class Question
    {
        public int Id { get; set; }
        public int QuizId { get; set; }
        public Quiz Quiz { get; set; }
        public required string QuestionTitle { get; set; }
        // One of QuestionTypes. It is chosen when the question is made and never changes.
        public string Type { get; set; } = QuestionTypes.Choice;
        // The four answers and the correct one belong to a choice question; other types have none.
        public string? FirstAnswer { get; set; }
        public string? SecondAnswer { get; set; }
        public string? ThirdAnswer { get; set; }
        public string? FourthAnswer { get; set; }
        // Which of the four answers is the correct one, 1 to 4.
        public int? CorrectOption { get; set; }
        // The pairs of a matching question or the items of an ordering one.
        public QuestionContent? Content { get; set; }
        public string? HintText { get; set; }
        public string? ExploreMore { get; set; }
        // Removed from a quiz that had been played: out of the editor and of new plays,
        // kept for the answers already given to it.
        public bool IsRetired { get; set; }
        // Its place in the quiz. Questions stored before this have 0 and keep their id order.
        public int Position { get; set; }
        // The passage the question is asked about, if it has one.
        public int? SourceTextId { get; set; }
        public SourceText? SourceText { get; set; }

    }
}
