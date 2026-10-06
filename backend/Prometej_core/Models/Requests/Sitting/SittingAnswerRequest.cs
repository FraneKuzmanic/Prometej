using System.ComponentModel.DataAnnotations;

namespace Prometej_core.Models.Requests.Sitting
{
    // One question's answer in a sitting, replacing what was saved for it before.
    public class SittingAnswerRequest
    {
        public int QuestionId { get; set; }
        // Numbers in the order the sitting showed the lists, 0 for a point left empty: one for
        // a choice question, one per pair, one per place.
        [Required, MaxLength(6)]
        public required int[] Given { get; set; }
    }
}
