using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Prometej_core.Models.efModels
{
    public class Answer
    {
        public int Id { get; set; }
        public int QuizGameId { get; set; }
        public QuizGame QuizGame { get; set; }
        public int QuestionId { get; set; }
        public Question Question { get; set; }
        public string AnswerText { get; set; }
        public string CorrectAnswer { get; set; }
    }
}
