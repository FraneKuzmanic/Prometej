using Prometej_core.Models.Requests.Quiz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prometej_core.Models.Dtos
{
    public class QuizCreateDto
    {
        public QuizCreateRequest Quiz { get; set; }
        public List<QuestionCreateRequest> Questions { get; set; }
    }

    public class  QuizEditDto
    {
        public QuizEditRequest Quiz { get; set; }
        public List<QuestionEditRequest>? Questions { get; set; }

    }
}
