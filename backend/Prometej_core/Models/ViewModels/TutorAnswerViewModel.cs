namespace Prometej_core.Models.ViewModels
{
    public class TutorAnswerViewModel
    {
        // "answer", "not_covered", "declined" or "unverified".
        public required string Kind { get; set; }
        public required string Answer { get; set; }
        public required List<TutorCitationViewModel> Citations { get; set; }
    }

    public class TutorCitationViewModel
    {
        public int PeriodId { get; set; }
        public required string PeriodName { get; set; }
        // The id the Period page gives the heading, so a citation is an address.
        public required string SectionId { get; set; }
        public required string SectionTitle { get; set; }
        public required string Quote { get; set; }
    }
}
