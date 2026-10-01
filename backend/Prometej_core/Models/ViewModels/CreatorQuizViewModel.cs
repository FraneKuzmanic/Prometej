using Prometej_core.Models.Base;

namespace Prometej_core.Models.ViewModels
{
    // A quiz as its creator lists it: with how many stored plays deleting it would take along.
    public class CreatorQuizViewModel : QuizBaseModel
    {
        public int QuizGameCount { get; set; }
    }
}
