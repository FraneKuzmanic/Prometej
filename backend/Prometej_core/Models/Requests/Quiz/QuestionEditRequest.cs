using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prometej_core.Models.Requests.Quiz
{
    public class QuestionEditRequest : QuestionCreateRequest
    {
        // 0 for a question added in this edit.
        public int Id { get; set; }
    }
}
