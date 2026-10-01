using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prometej_core.Models.Requests.Quiz
{
    public class QuizCreateRequest
    {
        public string Title { get; set; }
        public bool IsPrivate { get; set; }
        public int CreatorId { get; set; }
        public int entryCode { get; set; }
    }
}
