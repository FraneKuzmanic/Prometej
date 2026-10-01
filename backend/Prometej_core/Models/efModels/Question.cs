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
        public string FirstAnswer { get; set; }
        public string SecondAnswer { get; set; }
        public string ThirdAnswer { get; set; }
        public string FourthAnswer { get; set; }
        // Which of the four answers is the correct one, 1 to 4.
        public int CorrectOption { get; set; }
        public string? HintText { get; set; }
        public string? ExploreMore { get; set; }
        // Removed from a quiz that had been played: out of the editor and of new plays,
        // kept for the answers already given to it.
        public bool IsRetired { get; set; }

    }
}
