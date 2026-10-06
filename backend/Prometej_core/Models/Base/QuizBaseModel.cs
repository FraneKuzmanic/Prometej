using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prometej_core.Models.Base
{
    public class QuizBaseModel
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public required string CreatorName { get; set; }
        public bool IsPrivate { get; set; }
        public int? EntryCode { get; set; }
        public int? PeriodId { get; set; }
        public string? PeriodName { get; set; }
        public bool IsTest { get; set; }
        public int? TimeLimitMinutes { get; set; }
        public DateTime? ClosesAt { get; set; }
        public int QuestionCount { get; set; }
    }
}
