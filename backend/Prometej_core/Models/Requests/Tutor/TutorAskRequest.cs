using System.ComponentModel.DataAnnotations;

namespace Prometej_core.Models.Requests.Tutor
{
    public class TutorAskRequest
    {
        [Required, StringLength(500)]
        public required string Question { get; set; }
        // The conversation so far, as the browser kept it: the server stores none of it.
        [MaxLength(20), NoNullItems]
        public List<TutorTurnRequest> History { get; set; } = [];
        // The Period being read, when the question is asked on a Period's page.
        public int? PeriodId { get; set; }
    }

    public class TutorTurnRequest
    {
        [Required, RegularExpression("^(user|assistant)$")]
        public string? Role { get; set; }
        [Required, StringLength(4000)]
        public string? Content { get; set; }
    }
}
