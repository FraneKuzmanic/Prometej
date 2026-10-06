namespace Prometej_core.Models.ViewModels
{
    public class TutorDraftsViewModel
    {
        public required List<TutorDraftViewModel> Drafts { get; set; }
        // How many drafts were written and not offered: one the quiz rules would refuse, or
        // one whose quote is not in the section.
        public int Dropped { get; set; }
    }

    // A question nobody has saved: the teacher adds it to a quiz, changes it or leaves it.
    public class TutorDraftViewModel
    {
        public required string QuestionTitle { get; set; }
        public required string FirstAnswer { get; set; }
        public required string SecondAnswer { get; set; }
        public required string ThirdAnswer { get; set; }
        public required string FourthAnswer { get; set; }
        public int CorrectOption { get; set; }
        // The sentence of the section that makes the correct option right.
        public required string Quote { get; set; }
        // False when a second reading of the section did not pick the correct option.
        public bool Agrees { get; set; }
    }
}
