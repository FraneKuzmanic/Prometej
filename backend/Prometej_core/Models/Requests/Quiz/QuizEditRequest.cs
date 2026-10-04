using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prometej_core.Models.Requests.Quiz
{
    public class QuizEditRequest
    {
        public int Id { get; set; }
        [Required, StringLength(100)]
        public required string Title { get; set; }
        public bool IsPrivate { get; set; }
        // With the title and the visibility this is the Quiz's whole header, so left out
        // means no Period.
        public int? PeriodId { get; set; }
    }
}
