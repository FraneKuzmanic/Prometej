using Prometej_core.Models.Requests;
using Prometej_core.Models.Requests.Quiz;
using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prometej_core.Models.Dtos
{
    public class QuizCreateDto
    {
        [Required]
        public required QuizCreateRequest Quiz { get; set; }
        [Required, MinLength(1), NoNullItems]
        public required List<QuestionCreateRequest> Questions { get; set; }
    }

    public class  QuizEditDto
    {
        [Required]
        public required QuizEditRequest Quiz { get; set; }
        // Left out: the questions stay as they are. Sent: this is the whole set.
        [MinLength(1), NoNullItems]
        public List<QuestionEditRequest>? Questions { get; set; }
    }
}
