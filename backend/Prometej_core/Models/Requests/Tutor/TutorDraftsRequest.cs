using System.ComponentModel.DataAnnotations;

namespace Prometej_core.Models.Requests.Tutor
{
    public class TutorDraftsRequest
    {
        [Range(1, int.MaxValue)]
        public int PeriodId { get; set; }
        // The id the Period page gives a heading of the Period's text.
        [Required, RegularExpression(@"^odjeljak-\d{1,4}$")]
        public required string SectionId { get; set; }
    }
}
