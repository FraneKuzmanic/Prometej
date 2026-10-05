using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prometej_core.Models.efModels
{
    public class Answer
    {
        public int Id { get; set; }
        public int QuizGameId { get; set; }
        public QuizGame QuizGame { get; set; }
        public int QuestionId { get; set; }
        public Question Question { get; set; }
        // The question's title and explore more text and the two answers, all as they were
        // when the quiz was played.
        public string QuestionTitle { get; set; }
        public string? ExploreMore { get; set; }
        public string AnswerText { get; set; }
        public string CorrectAnswer { get; set; }
        // A question worth several points has a row for each. A row of a matching question
        // names the left-hand item it is about, a row of an ordering question the place
        // (from 1); a row with neither is the one row of a choice question.
        public string? Item { get; set; }
        public int? Place { get; set; }
        // Its ordinal in the quiz game: the questions in the order they were played, and a
        // question's rows in order.
        public int Position { get; set; }
        // The passage the question was played beside, in the version shown then.
        public int? SourceTextId { get; set; }
    }
}
