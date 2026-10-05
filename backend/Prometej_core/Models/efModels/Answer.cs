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
        // Its ordinal in the quiz game: the questions in the order they were played, and a
        // question's rows in order.
        public int Position { get; set; }
    }
}
