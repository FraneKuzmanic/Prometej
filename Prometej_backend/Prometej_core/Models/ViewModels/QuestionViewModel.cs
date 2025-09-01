using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prometej_core.Models.ViewModels
{
    public class QuestionViewModel
    {
        public int Id { get; set; }
        public int QuizId { get; set; }
        public string QuestionTitle { get; set; }
        public string FirstAnswer { get; set; }
        public string SecondAnswer { get; set; }
        public string ThirdAnswer { get; set; }
        public string FourthAnswer { get; set; }
        public string CorrectAnswer { get; set; }
        public string HintText { get; set; }
        public string ExploreMore { get; set; }
    }
}
