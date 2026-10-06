using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prometej_core.Models.Requests.Quiz
{
    public class QuizCreateRequest
    {
        [Required, StringLength(100)]
        public required string Title { get; set; }
        public bool IsPrivate { get; set; }
        public int? PeriodId { get; set; }
        // A test is a private quiz sat once by each student, with no answer shown before its end.
        public bool IsTest { get; set; }
        // How long a sitting may take. Left out: no limit.
        [Range(1, 300)]
        public int? TimeLimitMinutes { get; set; }
        // When a test closes. Kept only on a test; left out: when its creator closes it.
        public DateTimeOffset? ClosesAt { get; set; }
    }
}
